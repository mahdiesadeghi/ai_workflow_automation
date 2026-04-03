using WorkflowAutomation.Domain.Entities;
using WorkflowAutomation.Domain.ValueObjects;

namespace WorkflowAutomation.Application.Interfaces;

/// <summary>
/// Orchestrates the execution of a workflow through its defined steps,
/// coordinating scraping, AI analysis, and approval gates.
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    /// Executes all steps of the given workflow and returns the final result.
    /// </summary>
    /// <param name="workflow">The workflow to execute.</param>
    /// <returns>The analysis result produced by the workflow.</returns>
    Task<WorkflowResult> ExecuteWorkflowAsync(Workflow workflow);
}
