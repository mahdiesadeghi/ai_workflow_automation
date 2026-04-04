using WorkflowAutomation.Domain.Enums;
using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Domain.Entities;

/// <summary>
/// Root aggregate representing an AI-driven contract analysis workflow.
/// </summary>
public class Workflow
{
    public Guid Id { get; private set; }

    public WorkflowStatus Status { get; private set; }

    public ContractInput InputData { get; private set; }

    public WorkflowResult? Result { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// The execution mode for this workflow: "dotnet" (in-process) or "windmill" (Windmill engine).
    /// </summary>
    public string ExecutionMode { get; private set; } = "dotnet";

    private readonly List<WorkflowStep> _steps = new();

    public IReadOnlyList<WorkflowStep> Steps => _steps.AsReadOnly();

    private Workflow() { InputData = null!; } // EF Core

    public Workflow(ContractInput inputData, string executionMode = "dotnet")
    {
        Id = Guid.NewGuid();
        InputData = inputData ?? throw new ArgumentNullException(nameof(inputData));
        ExecutionMode = executionMode is "windmill" or "dotnet" ? executionMode : "dotnet";
        Status = WorkflowStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        _steps.Add(new WorkflowStep(Id, "input-validation", 1));
        _steps.Add(new WorkflowStep(Id, "data-normalization", 2));
        _steps.Add(new WorkflowStep(Id, "provider-scraping", 3));
        _steps.Add(new WorkflowStep(Id, "ai-analysis", 4));
        _steps.Add(new WorkflowStep(Id, "decision", 5));
        _steps.Add(new WorkflowStep(Id, "human-approval", 6));
    }

    /// <summary>
    /// Transitions the workflow to the Running state.
    /// </summary>
    public void Start()
    {
        if (Status != WorkflowStatus.Pending)
            throw new InvalidOperationException($"Cannot start a workflow in '{Status}' status.");

        Status = WorkflowStatus.Running;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes the workflow with the given analysis result.
    /// </summary>
    public void Complete(WorkflowResult result)
    {
        if (Status != WorkflowStatus.Running && Status != WorkflowStatus.Approved)
            throw new InvalidOperationException($"Cannot complete a workflow in '{Status}' status.");

        Result = result ?? throw new ArgumentNullException(nameof(result));
        Status = WorkflowStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the workflow as failed with an error recorded in the result.
    /// </summary>
    public void Fail(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message is required.", nameof(error));

        Result = new WorkflowResult(
            recommendation: "error",
            reasoning: error,
            suggestedOffer: null,
            estimatedSavings: 0m,
            analyzedAt: DateTime.UtcNow);

        Status = WorkflowStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the workflow to await human approval.
    /// </summary>
    public void RequestApproval()
    {
        if (Status != WorkflowStatus.Running)
            throw new InvalidOperationException($"Cannot request approval for a workflow in '{Status}' status.");

        Status = WorkflowStatus.AwaitingApproval;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Approves the workflow, allowing it to continue execution.
    /// </summary>
    public void Approve()
    {
        if (Status != WorkflowStatus.AwaitingApproval)
            throw new InvalidOperationException($"Cannot approve a workflow in '{Status}' status.");

        Status = WorkflowStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the workflow, halting further execution.
    /// </summary>
    public void Reject()
    {
        if (Status != WorkflowStatus.AwaitingApproval)
            throw new InvalidOperationException($"Cannot reject a workflow in '{Status}' status.");

        Status = WorkflowStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a step to the workflow. Used internally or for dynamic step injection.
    /// </summary>
    public void AddStep(WorkflowStep step)
    {
        _steps.Add(step ?? throw new ArgumentNullException(nameof(step)));
        UpdatedAt = DateTime.UtcNow;
    }
}
