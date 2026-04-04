using FluentValidation;
using WorkflowAutomation.Application.Commands;

namespace WorkflowAutomation.Application.Validators;

/// <summary>
/// Validation rules for the <see cref="StartWorkflowCommand"/>.
/// </summary>
public sealed class StartWorkflowCommandValidator : AbstractValidator<StartWorkflowCommand>
{
    public StartWorkflowCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage("Provider is required.")
            .MaximumLength(200)
            .WithMessage("Provider must not exceed 200 characters.");

        RuleFor(x => x.CurrentPrice)
            .GreaterThan(0)
            .WithMessage("Current price must be greater than zero.");

        RuleFor(x => x.Duration)
            .GreaterThan(0)
            .WithMessage("Duration must be greater than zero months.");

        RuleFor(x => x.PlanType)
            .NotEmpty()
            .WithMessage("Plan type is required.")
            .Must(pt => pt is "electricity" or "gas" or "dual")
            .WithMessage("Plan type must be 'electricity', 'gas', or 'dual'.");

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage("Customer name is required.")
            .MaximumLength(300)
            .WithMessage("Customer name must not exceed 300 characters.");
    }
}
