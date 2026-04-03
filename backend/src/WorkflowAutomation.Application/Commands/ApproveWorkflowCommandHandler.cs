using MediatR;
using Microsoft.Extensions.Logging;
using WorkflowAutomation.Application.DTOs;
using WorkflowAutomation.Application.Interfaces;
using WorkflowAutomation.Application.Mapping;
using WorkflowAutomation.Domain.Interfaces;

namespace WorkflowAutomation.Application.Commands;

/// <summary>
/// Handles the <see cref="ApproveWorkflowCommand"/> by approving or rejecting the workflow
/// and resuming orchestration if approved.
/// </summary>
public sealed class ApproveWorkflowCommandHandler : IRequestHandler<ApproveWorkflowCommand, WorkflowResponse>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly ILogger<ApproveWorkflowCommandHandler> _logger;

    public ApproveWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IWorkflowOrchestrator orchestrator,
        ILogger<ApproveWorkflowCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<WorkflowResponse> Handle(ApproveWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.WorkflowId} not found.");

        if (request.Approved)
        {
            workflow.Approve();
            _logger.LogInformation("Workflow {WorkflowId} approved", workflow.Id);

            await _workflowRepository.UpdateAsync(workflow, cancellationToken);

            // Resume orchestration after approval (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _orchestrator.ExecuteWorkflowAsync(workflow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Workflow {WorkflowId} post-approval execution failed", workflow.Id);
                }
            }, CancellationToken.None);
        }
        else
        {
            workflow.Reject();
            _logger.LogInformation("Workflow {WorkflowId} rejected. Comment: {Comment}",
                workflow.Id, request.Comment);

            await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        }

        return WorkflowMapper.ToResponse(workflow);
    }
}
