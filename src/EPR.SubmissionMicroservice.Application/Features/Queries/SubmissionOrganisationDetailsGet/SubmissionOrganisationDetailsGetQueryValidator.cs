namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionOrganisationDetailsGet;

using FluentValidation;

public class SubmissionOrganisationDetailsGetQueryValidator : AbstractValidator<SubmissionOrganisationDetailsGetQuery>
{
    public SubmissionOrganisationDetailsGetQueryValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty().NotEqual(Guid.Empty);
        RuleFor(x => x.BlobName).NotEmpty();
    }
}
