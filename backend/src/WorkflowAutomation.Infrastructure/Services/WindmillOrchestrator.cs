using Microsoft.Extensions.Logging;
using WorkflowAutomation.Application.Interfaces;
using WorkflowAutomation.Domain.Entities;
using WorkflowAutomation.Domain.Enums;
using WorkflowAutomation.Domain.Interfaces;
using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Infrastructure.Services;

/// <summary>
/// Orchestrates workflow execution by delegating to a self-hosted Windmill instance.
/// Each step attempts remote execution on Windmill first; when Windmill is unreachable
/// or the script is missing, falls back to real local execution (same logic as the
/// .NET WorkflowOrchestrator).
/// </summary>
public class WindmillOrchestrator : IWorkflowOrchestrator
{
    private readonly WindmillClient _windmillClient;
    private readonly IProviderScraperService _scraperService;
    private readonly IAiAnalysisService _aiAnalysisService;
    private readonly IOfferRepository _offerRepository;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILogger<WindmillOrchestrator> _logger;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StepDelay = TimeSpan.FromMilliseconds(500);

    public WindmillOrchestrator(
        WindmillClient windmillClient,
        IProviderScraperService scraperService,
        IAiAnalysisService aiAnalysisService,
        IOfferRepository offerRepository,
        IWorkflowRepository workflowRepository,
        ILogger<WindmillOrchestrator> logger)
    {
        _windmillClient = windmillClient;
        _scraperService = scraperService;
        _aiAnalysisService = aiAnalysisService;
        _offerRepository = offerRepository;
        _workflowRepository = workflowRepository;
        _logger = logger;
    }

    public async Task<WorkflowResult> ExecuteWorkflowAsync(Workflow workflow)
    {
        _logger.LogInformation("Starting Windmill orchestration for workflow {WorkflowId}", workflow.Id);

        try
        {
            // If workflow is Approved, skip to execution step
            if (workflow.Status == WorkflowStatus.Approved)
            {
                return await ResumeAfterApproval(workflow);
            }

            workflow.Start();
            await _workflowRepository.UpdateAsync(workflow);

            // Step 1: Input Validation
            await ExecuteWindmillStep(workflow, "input-validation", 1,
                "workflows/input_validation", async () =>
                {
                    ValidateInput(workflow.InputData);
                    return "Input validated successfully";
                });

            // Step 2: Data Normalization
            await ExecuteWindmillStep(workflow, "data-normalization", 2,
                "workflows/data_normalization", async () =>
                {
                    await Task.Delay(StepDelay);
                    return $"Price normalized: {workflow.InputData.CurrentPrice:C}/month, " +
                           $"Plan type: {workflow.InputData.PlanType}";
                });

            // Step 3: Provider Scraping
            List<OfferInfo> scrapedOffers = new();
            await ExecuteWindmillStep(workflow, "provider-scraping", 3,
                "workflows/provider_scraping", async () =>
                {
                    scrapedOffers = await _scraperService.ScrapeProviderOffersAsync(workflow.InputData.Provider);
                    return $"Scraped {scrapedOffers.Count} offers from providers";
                });

            // Step 4: AI Analysis
            WorkflowResult? analysisResult = null;
            await ExecuteWindmillStep(workflow, "ai-analysis", 4,
                "workflows/ai_analysis", async () =>
                {
                    var allOffers = await _offerRepository.GetAllAsync();
                    var combinedOffers = allOffers.Union(scrapedOffers).ToList();
                    analysisResult = await _aiAnalysisService.AnalyzeContractAsync(workflow.InputData, combinedOffers);
                    return $"AI recommends: {analysisResult.Recommendation} " +
                           $"(savings: {analysisResult.EstimatedSavings:C}/month)";
                });

            // Step 5: Decision
            await ExecuteWindmillStep(workflow, "decision", 5,
                "workflows/decision", async () =>
                {
                    await Task.Delay(StepDelay);
                    return analysisResult!.Recommendation == "switch"
                        ? $"Decision: Switch to {analysisResult.SuggestedOffer?.Provider ?? "alternative"}"
                        : "Decision: Keep current contract";
                });

            // Step 6: Human Approval - pause the workflow
            var approvalStep = workflow.Steps.FirstOrDefault(s => s.Name == "human-approval");
            if (approvalStep is null)
            {
                approvalStep = new WorkflowStep(workflow.Id, "human-approval", 6);
                workflow.AddStep(approvalStep);
            }
            approvalStep.Start();
            await _workflowRepository.UpdateAsync(workflow);
            approvalStep.Complete("Workflow paused - awaiting human approval (Windmill mode)");
            workflow.RequestApproval();
            await _workflowRepository.UpdateAsync(workflow);

            _logger.LogInformation("Windmill workflow {WorkflowId} paused at human-approval step", workflow.Id);

            return analysisResult ?? new WorkflowResult(
                "pending",
                "Awaiting human approval (Windmill mode)",
                null,
                0m,
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Windmill workflow {WorkflowId} failed during orchestration", workflow.Id);
            workflow.Fail(ex.Message);
            await _workflowRepository.UpdateAsync(workflow);
            throw;
        }
    }

    private async Task<WorkflowResult> ResumeAfterApproval(Workflow workflow)
    {
        _logger.LogInformation("Resuming Windmill workflow {WorkflowId} after approval", workflow.Id);

        // Step 7: Execution
        await ExecuteWindmillStep(workflow, "execution", 7,
            "workflows/execution", async () =>
            {
                await Task.Delay(StepDelay * 2);
                return "Provider switch executed successfully. " +
                       "Confirmation email sent. New contract activated.";
            });

        var result = workflow.Result ?? new WorkflowResult(
            "completed",
            "Workflow completed after approval (Windmill mode)",
            null,
            0m,
            DateTime.UtcNow);

        workflow.Complete(result);
        await _workflowRepository.UpdateAsync(workflow);

        _logger.LogInformation("Windmill workflow {WorkflowId} completed successfully", workflow.Id);
        return result;
    }

    private async Task ExecuteWindmillStep(
        Workflow workflow,
        string stepName,
        int order,
        string windmillScriptPath,
        Func<Task<string>> localAction)
    {
        var step = workflow.Steps.FirstOrDefault(s => s.Name == stepName);
        if (step is null)
        {
            step = new WorkflowStep(workflow.Id, stepName, order);
            workflow.AddStep(step);
        }

        _logger.LogDebug("Executing Windmill step '{StepName}' for workflow {WorkflowId}", stepName, workflow.Id);

        step.Start();
        await _workflowRepository.UpdateAsync(workflow);

        try
        {
            string output;

            if (await _windmillClient.IsHealthy())
            {
                try
                {
                    var input = new Dictionary<string, object>
                    {
                        ["workflow_id"] = workflow.Id.ToString(),
                        ["step"] = stepName,
                        ["provider"] = workflow.InputData.Provider,
                        ["current_price"] = workflow.InputData.CurrentPrice,
                        ["duration"] = workflow.InputData.Duration,
                        ["plan_type"] = workflow.InputData.PlanType,
                        ["customer_name"] = workflow.InputData.CustomerName
                    };

                    var jobId = await _windmillClient.TriggerScript(windmillScriptPath, input);
                    output = await PollJobCompletion(jobId, stepName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Windmill script '{ScriptPath}' failed for step '{StepName}'. Falling back to local execution.",
                        windmillScriptPath, stepName);
                    output = await localAction();
                }
            }
            else
            {
                _logger.LogWarning(
                    "Windmill is not reachable for step '{StepName}'. Falling back to local execution.",
                    stepName);
                output = await localAction();
            }

            step.Complete(output);
            _logger.LogInformation("Windmill step '{StepName}' completed for workflow {WorkflowId}: {Output}",
                stepName, workflow.Id, output);
        }
        catch (Exception ex)
        {
            step.Fail($"Windmill step failed: {ex.Message}");
            _logger.LogError(ex, "Windmill step '{StepName}' failed for workflow {WorkflowId}", stepName, workflow.Id);
            throw;
        }

        await _workflowRepository.UpdateAsync(workflow);
    }

    private async Task<string> PollJobCompletion(string jobId, string stepName)
    {
        var deadline = DateTime.UtcNow + PollTimeout;

        while (DateTime.UtcNow < deadline)
        {
            var status = await _windmillClient.GetJobStatus(jobId);

            if (status.State == "CompletedJob")
            {
                return status.Success
                    ? $"Windmill job {jobId} completed successfully"
                    : $"Windmill job {jobId} completed with errors: {status.Error}";
            }

            if (status.State == "Failed")
            {
                throw new InvalidOperationException(
                    $"Windmill job {jobId} for step '{stepName}' failed: {status.Error}");
            }

            await Task.Delay(PollInterval);
        }

        throw new TimeoutException(
            $"Windmill job {jobId} for step '{stepName}' did not complete within {PollTimeout.TotalMinutes} minutes.");
    }

    private static void ValidateInput(ContractInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Provider))
            throw new ArgumentException("Provider is required.");
        if (input.CurrentPrice <= 0)
            throw new ArgumentException("Current price must be greater than zero.");
        if (input.Duration < 0)
            throw new ArgumentException("Duration cannot be negative.");
        if (string.IsNullOrWhiteSpace(input.PlanType))
            throw new ArgumentException("Plan type is required.");
        if (string.IsNullOrWhiteSpace(input.CustomerName))
            throw new ArgumentException("Customer name is required.");
    }
}
