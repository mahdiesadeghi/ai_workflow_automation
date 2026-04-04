using MediatR;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ApproveWorkflowCommandHandler> _logger;

    public ApproveWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ApproveWorkflowCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _serviceScopeFactory = serviceScopeFactory;
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

            var workflowId = workflow.Id;

            // Resume orchestration after approval in a new DI scope so that
            // scoped services (DbContext, repositories) stay alive for the
            // entire duration of the background work.
            _ = Task.Run(async () =>
            {
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IWorkflowOrchestrator>();
                var repository = scope.ServiceProvider.GetRequiredService<IWorkflowRepository>();

                try
                {
                    var freshWorkflow = await repository.GetByIdAsync(workflowId);
                    if (freshWorkflow is null)
                    {
                        _logger.LogError("Workflow {WorkflowId} not found when resuming orchestration", workflowId);
                        return;
                    }

                    await orchestrator.ExecuteWorkflowAsync(freshWorkflow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Workflow {WorkflowId} post-approval execution failed", workflowId);
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
