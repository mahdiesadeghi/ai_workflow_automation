using WorkflowAutomation.Domain.Enums;
using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Application.DTOs;

/// <summary>
/// DTO representing a workflow returned to the client.
/// </summary>
public sealed class WorkflowResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public ContractInput InputData { get; set; } = null!;
    public WorkflowResult? Result { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ExecutionMode { get; set; } = "dotnet";
    public List<WorkflowStepResponse> Steps { get; set; } = new();
}

/// <summary>
/// DTO representing a single workflow step returned to the client.
/// </summary>
public sealed class WorkflowStepResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Output { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Order { get; set; }
}
