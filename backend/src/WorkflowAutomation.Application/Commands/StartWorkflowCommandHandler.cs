using MediatR;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<StartWorkflowCommandHandler> _logger;

    public StartWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<StartWorkflowCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _serviceScopeFactory = serviceScopeFactory;
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

        var workflow = new Workflow(input, request.ExecutionMode);

        await _workflowRepository.AddAsync(workflow, cancellationToken);

        _logger.LogInformation("Workflow {WorkflowId} created for customer {Customer}",
            workflow.Id, request.CustomerName);

        var workflowId = workflow.Id;
        var executionMode = request.ExecutionMode is "windmill" or "dotnet" ? request.ExecutionMode : "dotnet";

        // Fire and forget the orchestration in a new DI scope so that
        // scoped services (DbContext, repositories) stay alive for the
        // entire duration of the background work.
        _ = Task.Run(async () =>
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var orchestrator = scope.ServiceProvider.GetRequiredKeyedService<IWorkflowOrchestrator>(executionMode);
            var repository = scope.ServiceProvider.GetRequiredService<IWorkflowRepository>();

            try
            {
                var freshWorkflow = await repository.GetByIdAsync(workflowId);
                if (freshWorkflow is null)
                {
                    _logger.LogError("Workflow {WorkflowId} not found when starting orchestration", workflowId);
                    return;
                }

                await orchestrator.ExecuteWorkflowAsync(freshWorkflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow {WorkflowId} orchestration failed", workflowId);
            }
        }, CancellationToken.None);

        return WorkflowMapper.ToResponse(workflow);
    }
}
