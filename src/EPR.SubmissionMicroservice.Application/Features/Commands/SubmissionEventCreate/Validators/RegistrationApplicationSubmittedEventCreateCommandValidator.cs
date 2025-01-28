using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

public class RegistrationApplicationSubmittedEventCreateCommandValidator : AbstractValidator<RegistrationApplicationSubmittedEventCreateCommand>
{
    public RegistrationApplicationSubmittedEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));

        RuleFor(x => x.ApplicationReferenceNumber).NotNull().NotEmpty().WithMessage("'ApplicationReferenceNumber' is mandatory");

        RuleFor(x => x.SubmissionDate).NotNull().WithMessage("'SubmissionDate' is mandatory");
    }
}
