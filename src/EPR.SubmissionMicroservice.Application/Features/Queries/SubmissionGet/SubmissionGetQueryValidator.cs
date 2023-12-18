namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionGet;

using FluentValidation;

public class SubmissionGetQueryValidator : AbstractValidator<SubmissionGetQuery>
{
    public SubmissionGetQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().NotEqual(Guid.Empty);
        RuleFor(x => x.OrganisationId).NotEmpty().NotEqual(Guid.Empty);
    }
}