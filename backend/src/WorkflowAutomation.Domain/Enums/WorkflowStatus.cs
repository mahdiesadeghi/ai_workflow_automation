namespace WorkflowAutomation.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a workflow.
/// </summary>
public enum WorkflowStatus
{
    Pending,
    Running,
    AwaitingApproval,
    Approved,
    Completed,
    Failed,
    Rejected
}
