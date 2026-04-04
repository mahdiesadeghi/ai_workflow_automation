using MediatR;
using WorkflowAutomation.Application.DTOs;

namespace WorkflowAutomation.Application.Commands;

/// <summary>
/// Command to start a new contract analysis workflow.
/// </summary>
public sealed class StartWorkflowCommand : IRequest<WorkflowResponse>
{
    public string Provider { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public int Duration { get; set; }
    public string PlanType { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ExecutionMode { get; set; } = "dotnet";
}
