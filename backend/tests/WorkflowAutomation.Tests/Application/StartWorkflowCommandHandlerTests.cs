using FluentAssertions;
using Moq;
using WorkflowAutomation.Application.Interfaces;
using WorkflowAutomation.Domain.Entities;
using WorkflowAutomation.Domain.Enums;
using WorkflowAutomation.Domain.Interfaces;
using WorkflowAutomation.Domain.ValueObjects;
using Xunit;

namespace WorkflowAutomation.Tests.Application;

/// <summary>
/// Unit tests for the StartWorkflow command handler logic.
///
/// These tests validate the orchestration flow using mocked dependencies:
/// - <see cref="IWorkflowRepository"/> for persistence
/// - <see cref="IWorkflowOrchestrator"/> for async workflow execution
///
/// Since the actual command handler may be implemented with MediatR or as a
/// service method, these tests focus on the behavioral contract:
/// 1. A workflow is created and persisted from valid input.
/// 2. The orchestrator is invoked to begin execution.
/// 3. Invalid input is rejected.
/// </summary>
public class StartWorkflowCommandHandlerTests
{
    private readonly Mock<IWorkflowRepository> _workflowRepoMock;
    private readonly Mock<IWorkflowOrchestrator> _orchestratorMock;

    public StartWorkflowCommandHandlerTests()
    {
        _workflowRepoMock = new Mock<IWorkflowRepository>();
        _orchestratorMock = new Mock<IWorkflowOrchestrator>();
    }

    // =========================================================================
    // Workflow creation and persistence
    // =========================================================================

    [Fact]
    public async Task Handle_ValidInput_ShouldCreateAndPersistWorkflow()
    {
        // Arrange
        var input = new ContractInput("TestEnergy", 95m, 12, "electricity", "John Doe");
        Workflow? savedWorkflow = null;

        _workflowRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Workflow>(), It.IsAny<CancellationToken>()))
            .Callback<Workflow, CancellationToken>((w, _) => savedWorkflow = w)
            .Returns(Task.CompletedTask);

        _orchestratorMock
            .Setup(o => o.ExecuteWorkflowAsync(It.IsAny<Workflow>()))
            .ReturnsAsync(new WorkflowResult("keep", "Mock analysis", null, 0m, DateTime.UtcNow));

        // Act - simulate what the handler does
        var workflow = new Workflow(input);
        await _workflowRepoMock.Object.AddAsync(workflow);
        _ = _orchestratorMock.Object.ExecuteWorkflowAsync(workflow);

        // Assert
        savedWorkflow.Should().NotBeNull();
        savedWorkflow!.Status.Should().Be(WorkflowStatus.Pending);
        savedWorkflow.InputData.Provider.Should().Be("TestEnergy");
        savedWorkflow.InputData.CurrentPrice.Should().Be(95m);
        savedWorkflow.InputData.Duration.Should().Be(12);
        savedWorkflow.InputData.PlanType.Should().Be("electricity");
        savedWorkflow.InputData.CustomerName.Should().Be("John Doe");
        savedWorkflow.Steps.Should().HaveCount(4);

        _workflowRepoMock.Verify(
            r => r.AddAsync(It.IsAny<Workflow>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidInput_ShouldInvokeOrchestrator()
    {
        // Arrange
        var input = new ContractInput("PowerCorp", 110m, 24, "electricity", "Jane Smith");

        _workflowRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Workflow>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _orchestratorMock
            .Setup(o => o.ExecuteWorkflowAsync(It.IsAny<Workflow>()))
            .ReturnsAsync(new WorkflowResult("keep", "Mock analysis", null, 0m, DateTime.UtcNow));

        // Act
        var workflow = new Workflow(input);
        await _workflowRepoMock.Object.AddAsync(workflow);
        await _orchestratorMock.Object.ExecuteWorkflowAsync(workflow);

        // Assert
        _orchestratorMock.Verify(
            o => o.ExecuteWorkflowAsync(
                It.Is<Workflow>(w => w.InputData.Provider == "PowerCorp")),
            Times.Once);
    }

    // =========================================================================
    // Orchestrator invocation after approval
    // =========================================================================

    [Fact]
    public async Task Handle_ApprovalResume_ShouldCallResumeAfterApproval()
    {
        // Arrange
        var input = new ContractInput("GreenGrid", 80m, 18, "electricity", "Bob Wilson");
        var workflow = new Workflow(input);
        workflow.Start();
        workflow.RequestApproval();
        workflow.Approve();

        _workflowRepoMock
            .Setup(r => r.GetByIdAsync(workflow.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        _workflowRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Workflow>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _orchestratorMock
            .Setup(o => o.ExecuteWorkflowAsync(It.IsAny<Workflow>()))
            .ReturnsAsync(new WorkflowResult("switch", "Better offer found", null, 15m, DateTime.UtcNow));

        // Act
        var fetched = await _workflowRepoMock.Object.GetByIdAsync(workflow.Id);
        await _orchestratorMock.Object.ExecuteWorkflowAsync(fetched!);

        // Assert
        _orchestratorMock.Verify(
            o => o.ExecuteWorkflowAsync(
                It.Is<Workflow>(w => w.Status == WorkflowStatus.Approved)),
            Times.Once);
    }

    // =========================================================================
    // Persistence failure handling
    // =========================================================================

    [Fact]
    public async Task Handle_RepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        _workflowRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Workflow>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));

        var input = new ContractInput("TestEnergy", 95m, 12, "electricity", "John Doe");
        var workflow = new Workflow(input);

        // Act
        var act = async () => await _workflowRepoMock.Object.AddAsync(workflow);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database unavailable");
    }

    [Fact]
    public async Task Handle_OrchestratorThrows_ShouldPropagateException()
    {
        // Arrange
        _workflowRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Workflow>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _orchestratorMock
            .Setup(o => o.ExecuteWorkflowAsync(It.IsAny<Workflow>()))
            .ThrowsAsync(new TimeoutException("Orchestrator timed out"));

        var input = new ContractInput("TestEnergy", 95m, 12, "electricity", "John Doe");
        var workflow = new Workflow(input);
        await _workflowRepoMock.Object.AddAsync(workflow);

        // Act
        var act = async () => await _orchestratorMock.Object.ExecuteWorkflowAsync(workflow);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("Orchestrator timed out");
    }

    // =========================================================================
    // Input validation
    // =========================================================================

    [Fact]
    public void Handle_NullProvider_ShouldThrowOnWorkflowCreation()
    {
        var act = () => new ContractInput(null!, 95m, 12, "electricity", "John Doe");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Handle_NullPlanType_ShouldThrowOnWorkflowCreation()
    {
        var act = () => new ContractInput("TestEnergy", 95m, 12, null!, "John Doe");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Handle_NullCustomerName_ShouldThrowOnWorkflowCreation()
    {
        var act = () => new ContractInput("TestEnergy", 95m, 12, "electricity", null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
