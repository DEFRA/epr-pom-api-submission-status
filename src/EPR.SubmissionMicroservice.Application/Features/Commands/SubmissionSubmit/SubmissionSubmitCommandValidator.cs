namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;

using FluentValidation;

public class SubmissionSubmitCommandValidator : AbstractValidator<SubmissionSubmitCommand>
{
    public SubmissionSubmitCommandValidator()
    {
        RuleFor(p => p.SubmissionId).NotEmpty().WithMessage("Submission Id is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
    }
}