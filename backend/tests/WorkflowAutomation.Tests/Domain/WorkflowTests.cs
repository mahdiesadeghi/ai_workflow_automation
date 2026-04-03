using FluentAssertions;
using WorkflowAutomation.Domain.Entities;
using WorkflowAutomation.Domain.Enums;
using WorkflowAutomation.Domain.ValueObjects;
using Xunit;

namespace WorkflowAutomation.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="Workflow"/> aggregate root.
/// Validates creation, status transitions, and approval flow.
/// </summary>
public class WorkflowTests
{
    private static ContractInput CreateSampleInput() =>
        new("TestEnergy", 95m, 12, "electricity", "John Doe");

    // =========================================================================
    // Creation
    // =========================================================================

    [Fact]
    public void Constructor_ShouldSetPendingStatus()
    {
        var workflow = new Workflow(CreateSampleInput());

        workflow.Status.Should().Be(WorkflowStatus.Pending);
        workflow.Id.Should().NotBeEmpty();
        workflow.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_ShouldStoreInputData()
    {
        var input = CreateSampleInput();
        var workflow = new Workflow(input);

        workflow.InputData.Should().Be(input);
        workflow.InputData.Provider.Should().Be("TestEnergy");
        workflow.InputData.CurrentPrice.Should().Be(95m);
    }

    [Fact]
    public void Constructor_ShouldCreateFourDefaultSteps()
    {
        var workflow = new Workflow(CreateSampleInput());

        workflow.Steps.Should().HaveCount(4);
        workflow.Steps[0].Name.Should().Be("Scrape Provider Offers");
        workflow.Steps[1].Name.Should().Be("AI Contract Analysis");
        workflow.Steps[2].Name.Should().Be("Human Approval");
        workflow.Steps[3].Name.Should().Be("Finalize Recommendation");
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullInput()
    {
        var act = () => new Workflow(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // =========================================================================
    // Status transitions
    // =========================================================================

    [Fact]
    public void Start_FromPending_ShouldTransitionToRunning()
    {
        var workflow = new Workflow(CreateSampleInput());

        workflow.Start();

        workflow.Status.Should().Be(WorkflowStatus.Running);
        workflow.UpdatedAt.Should().BeOnOrAfter(workflow.CreatedAt);
    }

    [Fact]
    public void Start_FromRunning_ShouldThrow()
    {
        var workflow = new Workflow(CreateSampleInput());
        workflow.Start();

        var act = () => workflow.Start();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Running*");
    }

    [Fact]
    public void Complete_FromRunning_ShouldTransitionToCompleted()
    {
        var workflow = new Workflow(CreateSampleInput());
        workflow.Start();

        var result = new WorkflowResult("switch", "Better offer found", null, 15m, DateTime.UtcNow);
        workflow.Complete(result);

        workflow.Status.Should().Be(WorkflowStatus.Completed);
        workflow.Result.Should().Be(result);
    }

    [Fact]
    public void Complete_FromApproved_ShouldTransitionToCompleted()
    {
        var workflow = new Workflow(CreateSampleInput());
        workflow.Start();
        workflow.RequestApproval();
        workflow.Approve();

        var result = new WorkflowResult("switch", "Better offer found", null, 15m, DateTime.UtcNow);
        workflow.Complete(result);

        workflow.Status.Should().Be(WorkflowStatus.Completed);
    }

    [Fact]
    public void Complete_FromPending_ShouldThrow()
    {
        var workflow = new Workflow(CreateSampleInput());

        var result = new WorkflowResult("keep", "No action", null, 0m, DateTime.UtcNow);
        var act = () => workflow.Complete(result);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_WithNullResult_ShouldThrow()
    {
        var workflow = new Workflow(CreateSampleInput());
        workflow.Start();

        var act = () => workflow.Complete(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Fail_ShouldSetFailedStatusAndRecordError()
    {
        var workflow = new Workflow(CreateSampleInput());
        workflow.Start();

        workflow.Fail("Provider portal unavailable");

        workflow.Status.Should().Be(WorkflowStatus.Failed);
        workflow.Result.Should().NotBeNull();
        workflow.Result!.Recommendation.Should().Be("error");
        workflow.Result.Reasoning.Should().Be("Provider portal unavailable");
    }

    [Fact]
    public void Fail_WithEmptyError_ShouldThrow()
    {
        var workflow = new Workflow(CreateSampleInput());
        workflow.Start();

        var act = () => workflow.Fail("");

        act.Should().Throw<ArgumentException>();
    }

    // =========================================================================
    // Approval flow
    // =========================================================================

    [Fact]
    public void RequestApproval_FromRunning_ShouldTransitionToAwaitingApproval()
    {
        var workflow = new Workflow(CreateSampleInput());
        workflow.Start();

        workflow.RequestApproval();

        workflow.Status.Should().Be(WorkflowStatus.AwaitingApproval);
    }

    [Fact]
    public void RequestApproval_FromPending_ShouldThrow()
    {
        var workflow = new Workflow(CreateSampleInput());

        var act = () => workflow.RequestApproval();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_FromAwaitingApproval_ShouldTransitionToApproved()
    {
        var workflow = new Workflow(CreateSampleInput());
        workflow.Start();
        workflow.RequestApproval();

        workflow.Approve();

        workflow.Status.Should().Be(WorkflowStatus.Approved);
    }

    [Fact]
    public void Approve_FromRunning_ShouldThrow()
    {
        var workflow = new Workflow(CreateSampleInput());
        workflow.Start();

        var act = () => workflow.Approve();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_FromAwaitingApproval_ShouldTransitionToRejected()
    {
        var workflow = new Workflow(CreateSampleInput());
        workflow.Start();
        workflow.RequestApproval();

        workflow.Reject();

        workflow.Status.Should().Be(WorkflowStatus.Rejected);
    }

    [Fact]
    public void Reject_FromRunning_ShouldThrow()
    {
        var workflow = new Workflow(CreateSampleInput());
        workflow.Start();

        var act = () => workflow.Reject();

        act.Should().Throw<InvalidOperationException>();
    }

    // =========================================================================
    // Full lifecycle
    // =========================================================================

    [Fact]
    public void FullLifecycle_Pending_Running_AwaitingApproval_Approved_Completed()
    {
        var workflow = new Workflow(CreateSampleInput());

        workflow.Status.Should().Be(WorkflowStatus.Pending);

        workflow.Start();
        workflow.Status.Should().Be(WorkflowStatus.Running);

        workflow.RequestApproval();
        workflow.Status.Should().Be(WorkflowStatus.AwaitingApproval);

        workflow.Approve();
        workflow.Status.Should().Be(WorkflowStatus.Approved);

        var result = new WorkflowResult(
            "switch",
            "GreenGrid Solar Plus saves $23/mo",
            new OfferInfo("GreenGrid", 72m, new List<string> { "Solar offset credits" }, "Solar Plus"),
            23m,
            DateTime.UtcNow);
        workflow.Complete(result);

        workflow.Status.Should().Be(WorkflowStatus.Completed);
        workflow.Result!.EstimatedSavings.Should().Be(23m);
    }

    // =========================================================================
    // Step management
    // =========================================================================

    [Fact]
    public void AddStep_ShouldIncreaseStepCount()
    {
        var workflow = new Workflow(CreateSampleInput());
        var initialCount = workflow.Steps.Count;

        workflow.AddStep(new WorkflowStep(workflow.Id, "Extra Validation", 5));

        workflow.Steps.Should().HaveCount(initialCount + 1);
        workflow.Steps.Last().Name.Should().Be("Extra Validation");
    }

    [Fact]
    public void AddStep_WithNull_ShouldThrow()
    {
        var workflow = new Workflow(CreateSampleInput());

        var act = () => workflow.AddStep(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
