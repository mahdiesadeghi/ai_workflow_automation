using MediatR;
using WorkflowAutomation.Application.DTOs;

namespace WorkflowAutomation.Application.Queries;

/// <summary>
/// Query to retrieve a single workflow by its identifier.
/// </summary>
public sealed class GetWorkflowQuery : IRequest<WorkflowResponse?>
{
    public Guid Id { get; set; }

    public GetWorkflowQuery(Guid id)
    {
        Id = id;
    }
}
