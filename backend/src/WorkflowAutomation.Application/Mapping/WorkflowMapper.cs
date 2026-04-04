using WorkflowAutomation.Application.DTOs;
using WorkflowAutomation.Domain.Entities;

namespace WorkflowAutomation.Application.Mapping;

/// <summary>
/// Static mapper that converts domain entities to application DTOs.
/// </summary>
public static class WorkflowMapper
{
    /// <summary>
    /// Maps a <see cref="Workflow"/> entity to a <see cref="WorkflowResponse"/> DTO.
    /// </summary>
    public static WorkflowResponse ToResponse(Workflow workflow)
    {
        return new WorkflowResponse
        {
            Id = workflow.Id,
            Status = workflow.Status.ToString(),
            InputData = workflow.InputData,
            Result = workflow.Result,
            CreatedAt = workflow.CreatedAt,
            ExecutionMode = workflow.ExecutionMode,
            Steps = workflow.Steps
                .OrderBy(s => s.Order)
                .Select(ToStepResponse)
                .ToList()
        };
    }

    /// <summary>
    /// Maps a <see cref="WorkflowStep"/> entity to a <see cref="WorkflowStepResponse"/> DTO.
    /// </summary>
    public static WorkflowStepResponse ToStepResponse(WorkflowStep step)
    {
        return new WorkflowStepResponse
        {
            Id = step.Id,
            Name = step.Name,
            Status = step.Status.ToString(),
            Output = step.Output,
            StartedAt = step.StartedAt,
            CompletedAt = step.CompletedAt,
            Order = step.Order
        };
    }
}
