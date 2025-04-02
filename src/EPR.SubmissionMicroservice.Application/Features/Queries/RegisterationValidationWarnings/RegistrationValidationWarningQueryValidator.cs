using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.RegisterationValidationWarnings;

public class RegistrationValidationWarningQueryValidator : AbstractValidator<RegistrationValidationWarningQuery>
{
    public RegistrationValidationWarningQueryValidator(IQueryRepository<Submission> queryRepository)
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty()
            .NotEqual(Guid.Empty);

        RuleFor(x => x.SubmissionId)
            .NotEmpty()
            .NotEqual(Guid.Empty)
            .MustAsync(async (query, id, cancellation) =>
            {
                var submission = await queryRepository.GetByIdAsync(id, cancellation);
                return submission != null && submission.OrganisationId == query.OrganisationId;
            })
            .WithMessage("OrganisationId does not match organisation of the submission record");
    }
}
