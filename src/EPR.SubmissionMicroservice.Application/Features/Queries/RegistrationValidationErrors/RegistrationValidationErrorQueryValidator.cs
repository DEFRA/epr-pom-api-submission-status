using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.RegistrationValidationErrors;

using FluentValidation;

public class RegistrationValidationErrorQueryValidator : AbstractValidator<RegistrationValidationErrorQuery>
{
    public RegistrationValidationErrorQueryValidator(IQueryRepository<Submission> queryRepository)
    {
        RuleFor(p => p.OrganisationId)
            .NotEmpty().NotEqual(Guid.Empty);
        RuleFor(p => p.SubmissionId)
            .NotEmpty().NotEqual(Guid.Empty)
            .MustAsync(async (query, id, cancellation) =>
            {
                var submission = await queryRepository.GetByIdAsync(id, cancellation);
                return submission != null && submission.OrganisationId == query.OrganisationId;
            })
            .WithMessage("OrganisationId does not match organisation of the submission record");
    }
}