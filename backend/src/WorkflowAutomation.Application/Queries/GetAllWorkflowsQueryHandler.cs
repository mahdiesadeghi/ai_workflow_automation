using MediatR;
using WorkflowAutomation.Application.DTOs;
using WorkflowAutomation.Application.Mapping;
using WorkflowAutomation.Domain.Interfaces;

namespace WorkflowAutomation.Application.Queries;

/// <summary>
/// Handles the <see cref="GetAllWorkflowsQuery"/> by retrieving all workflows from the repository.
/// </summary>
public sealed class GetAllWorkflowsQueryHandler : IRequestHandler<GetAllWorkflowsQuery, List<WorkflowResponse>>
{
    private readonly IWorkflowRepository _workflowRepository;

    public GetAllWorkflowsQueryHandler(IWorkflowRepository workflowRepository)
    {
        _workflowRepository = workflowRepository;
    }

    public async Task<List<WorkflowResponse>> Handle(GetAllWorkflowsQuery request, CancellationToken cancellationToken)
    {
        var workflows = await _workflowRepository.GetAllAsync(cancellationToken);
        return workflows.Select(WorkflowMapper.ToResponse).ToList();
    }
}
