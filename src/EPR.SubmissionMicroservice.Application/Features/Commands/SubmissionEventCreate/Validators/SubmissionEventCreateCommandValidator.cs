namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

using Data.Entities.Submission;
using Data.Repositories.Queries.Interfaces;
using FluentValidation;

public class SubmissionEventCreateCommandValidator : AbstractValidator<AbstractSubmissionEventCreateCommand>
{
    public SubmissionEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        RuleFor(p => p.SubmissionId)
            .NotNull().NotEmpty().NotEqual(Guid.Empty)
            .MustAsync(async (id, cancellation) => await queryRepository.GetByIdAsync(id, cancellation) != null)
            .WithMessage("Submission with id {PropertyValue} does not exist.");

        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.UserId).NotEmpty().NotEqual(default(Guid));
    }
}