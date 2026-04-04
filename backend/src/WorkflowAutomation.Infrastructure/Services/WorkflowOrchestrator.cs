using Microsoft.Extensions.Logging;
using WorkflowAutomation.Application.Interfaces;
using WorkflowAutomation.Domain.Entities;
using WorkflowAutomation.Domain.Enums;
using WorkflowAutomation.Domain.Interfaces;
using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Infrastructure.Services;

/// <summary>
/// Orchestrates workflow execution, simulating a Windmill.dev-style pipeline.
/// Each step is executed sequentially with status updates, timing, and logging.
/// </summary>
public class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly IProviderScraperService _scraperService;
    private readonly IAiAnalysisService _aiAnalysisService;
    private readonly IOfferRepository _offerRepository;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILogger<WorkflowOrchestrator> _logger;

    private static readonly TimeSpan StepDelay = TimeSpan.FromMilliseconds(500);

    public WorkflowOrchestrator(
        IProviderScraperService scraperService,
        IAiAnalysisService aiAnalysisService,
        IOfferRepository offerRepository,
        IWorkflowRepository workflowRepository,
        ILogger<WorkflowOrchestrator> logger)
    {
        _scraperService = scraperService;
        _aiAnalysisService = aiAnalysisService;
        _offerRepository = offerRepository;
        _workflowRepository = workflowRepository;
        _logger = logger;
    }

    public async Task<WorkflowResult> ExecuteWorkflowAsync(Workflow workflow)
    {
        _logger.LogInformation("Starting orchestration for workflow {WorkflowId}", workflow.Id);

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
            await ExecuteStep(workflow, "input-validation", 1, async () =>
            {
                ValidateInput(workflow.InputData);
                return "Input validated successfully";
            });

            // Step 2: Data Normalization
            await ExecuteStep(workflow, "data-normalization", 2, async () =>
            {
                await Task.Delay(StepDelay);
                return $"Price normalized: {workflow.InputData.CurrentPrice:C}/month, " +
                       $"Plan type: {workflow.InputData.PlanType}";
            });

            // Step 3: Provider Scraping
            List<OfferInfo> scrapedOffers = new();
            await ExecuteStep(workflow, "provider-scraping", 3, async () =>
            {
                scrapedOffers = await _scraperService.ScrapeProviderOffersAsync(workflow.InputData.Provider);
                return $"Scraped {scrapedOffers.Count} offers from providers";
            });

            // Step 4: AI Analysis
            WorkflowResult? analysisResult = null;
            await ExecuteStep(workflow, "ai-analysis", 4, async () =>
            {
                var allOffers = await _offerRepository.GetAllAsync();
                var combinedOffers = allOffers.Union(scrapedOffers).ToList();
                analysisResult = await _aiAnalysisService.AnalyzeContractAsync(workflow.InputData, combinedOffers);
                return $"AI recommends: {analysisResult.Recommendation} " +
                       $"(savings: {analysisResult.EstimatedSavings:C}/month)";
            });

            // Step 5: Decision
            await ExecuteStep(workflow, "decision", 5, async () =>
            {
                await Task.Delay(StepDelay);
                return analysisResult!.Recommendation == "switch"
                    ? $"Decision: Switch to {analysisResult.SuggestedOffer?.Provider ?? "alternative"}"
                    : "Decision: Keep current contract";
            });

            // Step 6: Human Approval - pause the workflow here
            await ExecuteStep(workflow, "human-approval", 6, async () =>
            {
                await Task.Delay(StepDelay);
                workflow.RequestApproval();
                await _workflowRepository.UpdateAsync(workflow);
                return "Workflow paused - awaiting human approval";
            });

            // Store the intermediate result for display while awaiting approval
            // Note: The workflow is already in AwaitingApproval status from step 6.
            // We store the result directly without calling Complete() to preserve the status.
            await _workflowRepository.UpdateAsync(workflow);

            _logger.LogInformation("Workflow {WorkflowId} paused at human-approval step", workflow.Id);

            // Return the analysis result; the workflow will resume when approved
            return analysisResult ?? new WorkflowResult(
                "pending",
                "Awaiting human approval",
                null,
                0m,
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow {WorkflowId} failed during orchestration", workflow.Id);
            workflow.Fail(ex.Message);
            await _workflowRepository.UpdateAsync(workflow);
            throw;
        }
    }

    private async Task<WorkflowResult> ResumeAfterApproval(Workflow workflow)
    {
        _logger.LogInformation("Resuming workflow {WorkflowId} after approval", workflow.Id);

        // Step 7: Execution - simulate the provider switch
        await ExecuteStep(workflow, "execution", 7, async () =>
        {
            await Task.Delay(StepDelay * 2); // Longer delay for execution simulation
            return "Provider switch executed successfully. " +
                   "Confirmation email sent. New contract activated.";
        });

        var result = workflow.Result ?? new WorkflowResult(
            "completed",
            "Workflow completed after approval",
            null,
            0m,
            DateTime.UtcNow);

        workflow.Complete(result);
        await _workflowRepository.UpdateAsync(workflow);

        _logger.LogInformation("Workflow {WorkflowId} completed successfully", workflow.Id);
        return result;
    }

    private async Task ExecuteStep(
        Workflow workflow,
        string stepName,
        int order,
        Func<Task<string>> action)
    {
        // Find existing step or create a new one
        var step = workflow.Steps.FirstOrDefault(s => s.Name == stepName);
        if (step is null)
        {
            step = new WorkflowStep(workflow.Id, stepName, order);
            workflow.AddStep(step);
        }

        _logger.LogDebug("Executing step '{StepName}' for workflow {WorkflowId}", stepName, workflow.Id);

        step.Start();
        await _workflowRepository.UpdateAsync(workflow);

        try
        {
            await Task.Delay(StepDelay); // Simulate processing time
            var output = await action();
            step.Complete(output);

            _logger.LogInformation("Step '{StepName}' completed for workflow {WorkflowId}: {Output}",
                stepName, workflow.Id, output);
        }
        catch (Exception ex)
        {
            step.Fail(ex.Message);
            _logger.LogError(ex, "Step '{StepName}' failed for workflow {WorkflowId}", stepName, workflow.Id);
            throw;
        }

        await _workflowRepository.UpdateAsync(workflow);
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
