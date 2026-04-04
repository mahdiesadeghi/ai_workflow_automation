using MediatR;
using Microsoft.Extensions.Logging;
using WorkflowAutomation.Application.DTOs;
using WorkflowAutomation.Application.Interfaces;
using WorkflowAutomation.Application.Mapping;
using WorkflowAutomation.Domain.Entities;
using WorkflowAutomation.Domain.Interfaces;
using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Application.Commands;

/// <summary>
/// Handles the <see cref="StartWorkflowCommand"/> by creating a new workflow,
/// persisting it, and kicking off the orchestration pipeline.
/// </summary>
public sealed class StartWorkflowCommandHandler : IRequestHandler<StartWorkflowCommand, WorkflowResponse>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly ILogger<StartWorkflowCommandHandler> _logger;

    public StartWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IWorkflowOrchestrator orchestrator,
        ILogger<StartWorkflowCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<WorkflowResponse> Handle(StartWorkflowCommand request, CancellationToken cancellationToken)
    {
        var input = new ContractInput(
            request.Provider,
            request.CurrentPrice,
            request.Duration,
            request.PlanType,
            request.CustomerName);

        var workflow = new Workflow(input);

        await _workflowRepository.AddAsync(workflow, cancellationToken);

        _logger.LogInformation("Workflow {WorkflowId} created for customer {Customer}",
            workflow.Id, request.CustomerName);

        // Fire and forget the orchestration (it will update the workflow as it progresses)
        _ = Task.Run(async () =>
        {
            try
            {
                await _orchestrator.ExecuteWorkflowAsync(workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow {WorkflowId} orchestration failed", workflow.Id);
            }
        }, CancellationToken.None);

        return WorkflowMapper.ToResponse(workflow);
    }
}
