using WorkflowAutomation.Domain.Entities;

namespace WorkflowAutomation.Domain.Interfaces;

/// <summary>
/// Repository abstraction for persisting and retrieving <see cref="Workflow"/> aggregates.
/// </summary>
public interface IWorkflowRepository
{
    /// <summary>
    /// Retrieves a workflow by its unique identifier.
    /// </summary>
    Task<Workflow?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all workflows.
    /// </summary>
    Task<List<Workflow>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new workflow.
    /// </summary>
    Task AddAsync(Workflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing workflow.
    /// </summary>
    Task UpdateAsync(Workflow workflow, CancellationToken cancellationToken = default);
}
