using MediatR;
using WorkflowAutomation.Application.DTOs;
using WorkflowAutomation.Application.Mapping;
using WorkflowAutomation.Domain.Interfaces;

namespace WorkflowAutomation.Application.Queries;

/// <summary>
/// Handles the <see cref="GetWorkflowQuery"/> by retrieving a workflow from the repository.
/// </summary>
public sealed class GetWorkflowQueryHandler : IRequestHandler<GetWorkflowQuery, WorkflowResponse?>
{
    private readonly IWorkflowRepository _workflowRepository;

    public GetWorkflowQueryHandler(IWorkflowRepository workflowRepository)
    {
        _workflowRepository = workflowRepository;
    }

    public async Task<WorkflowResponse?> Handle(GetWorkflowQuery request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.Id, cancellationToken);
        return workflow is null ? null : WorkflowMapper.ToResponse(workflow);
    }
}
