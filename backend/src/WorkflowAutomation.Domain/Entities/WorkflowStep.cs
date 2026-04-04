using WorkflowAutomation.Domain.Enums;

namespace WorkflowAutomation.Domain.Entities;

/// <summary>
/// Represents a single executable step within a workflow.
/// </summary>
public class WorkflowStep
{
    public Guid Id { get; private set; }

    public Guid WorkflowId { get; private set; }

    public string Name { get; private set; }

    public StepStatus Status { get; private set; }

    public string? Output { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public int Order { get; private set; }

    private WorkflowStep() { Name = string.Empty; } // EF Core

    public WorkflowStep(Guid workflowId, string name, int order)
    {
        Id = Guid.NewGuid();
        WorkflowId = workflowId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Order = order;
        Status = StepStatus.Pending;
    }

    /// <summary>
    /// Marks the step as running.
    /// </summary>
    public void Start()
    {
        Status = StepStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the step as completed with the given output.
    /// </summary>
    public void Complete(string? output = null)
    {
        Status = StepStatus.Completed;
        Output = output;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the step as failed with the given error output.
    /// </summary>
    public void Fail(string? error = null)
    {
        Status = StepStatus.Failed;
        Output = error;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the step as skipped.
    /// </summary>
    public void Skip()
    {
        Status = StepStatus.Skipped;
        CompletedAt = DateTime.UtcNow;
    }
}
