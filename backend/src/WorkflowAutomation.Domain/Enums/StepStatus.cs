namespace WorkflowAutomation.Domain.Enums;

/// <summary>
/// Represents the execution status of an individual workflow step.
/// </summary>
public enum StepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}
