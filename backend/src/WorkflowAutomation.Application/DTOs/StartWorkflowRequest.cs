namespace WorkflowAutomation.Application.DTOs;

/// <summary>
/// DTO for incoming requests to start a new contract analysis workflow.
/// </summary>
public sealed class StartWorkflowRequest
{
    public string Provider { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public int Duration { get; set; }
    public string PlanType { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
}
