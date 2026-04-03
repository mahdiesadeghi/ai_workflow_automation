using MediatR;
using WorkflowAutomation.Application.DTOs;

namespace WorkflowAutomation.Application.Commands;

/// <summary>
/// Command to approve or reject a workflow that is awaiting human approval.
/// </summary>
public sealed class ApproveWorkflowCommand : IRequest<WorkflowResponse>
{
    public Guid WorkflowId { get; set; }
    public bool Approved { get; set; }
    public string? Comment { get; set; }
}
