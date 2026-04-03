namespace WorkflowAutomation.Application.DTOs;

/// <summary>
/// DTO for incoming approval or rejection requests on a workflow.
/// </summary>
public sealed class ApprovalRequest
{
    public Guid WorkflowId { get; set; }
    public bool Approved { get; set; }
    public string? Comment { get; set; }
}
