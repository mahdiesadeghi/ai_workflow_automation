using MediatR;
using WorkflowAutomation.Application.DTOs;

namespace WorkflowAutomation.Application.Queries;

/// <summary>
/// Query to retrieve all workflows.
/// </summary>
public sealed class GetAllWorkflowsQuery : IRequest<List<WorkflowResponse>>
{
}
