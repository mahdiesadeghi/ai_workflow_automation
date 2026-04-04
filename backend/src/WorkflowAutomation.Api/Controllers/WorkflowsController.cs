using MediatR;
using Microsoft.AspNetCore.Mvc;
using WorkflowAutomation.Application.Commands;
using WorkflowAutomation.Application.DTOs;
using WorkflowAutomation.Application.Queries;

namespace WorkflowAutomation.Api.Controllers;

/// <summary>
/// Manages AI-driven energy contract analysis workflows.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WorkflowsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkflowsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Starts a new contract analysis workflow.
    /// </summary>
    /// <param name="request">The contract details to analyze.</param>
    /// <returns>The newly created workflow with initial status.</returns>
    [HttpPost("start")]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartWorkflow([FromBody] StartWorkflowRequest request)
    {
        var command = new StartWorkflowCommand
        {
            Provider = request.Provider,
            CurrentPrice = request.CurrentPrice,
            Duration = request.Duration,
            PlanType = request.PlanType,
            CustomerName = request.CustomerName,
            ExecutionMode = request.ExecutionMode
        };

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetWorkflow), new { id = result.Id }, result);
    }

    /// <summary>
    /// Retrieves a specific workflow by its identifier.
    /// </summary>
    /// <param name="id">The workflow ID.</param>
    /// <returns>The workflow details including current status and steps.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflow(Guid id)
    {
        var result = await _mediator.Send(new GetWorkflowQuery(id));

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Retrieves all workflows, ordered by creation date descending.
    /// </summary>
    /// <returns>A list of all workflows.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<WorkflowResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllWorkflows()
    {
        var result = await _mediator.Send(new GetAllWorkflowsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Approves or rejects a workflow that is awaiting human approval.
    /// </summary>
    /// <param name="id">The workflow ID to approve or reject.</param>
    /// <param name="request">The approval decision and optional comment.</param>
    /// <returns>The updated workflow after the approval decision.</returns>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveWorkflow(Guid id, [FromBody] ApprovalRequest request)
    {
        var command = new ApproveWorkflowCommand
        {
            WorkflowId = id,
            Approved = request.Approved,
            Comment = request.Comment
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
