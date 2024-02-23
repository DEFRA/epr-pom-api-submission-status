namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsPeriodGet;

using EPR.SubmissionMicroservice.Data.Enums;
using FluentValidation;

public class SubmissionsPeriodGetQueryValidator : AbstractValidator<SubmissionsPeriodGetQuery>
{
    public SubmissionsPeriodGetQueryValidator()
    {
        RuleFor(x => x.OrganisationId).NotEmpty().NotEqual(Guid.Empty);

        RuleFor(x => x.Type).NotEmpty().NotEqual(default(SubmissionType)).WithMessage("Please enter a valid submission type");

        RuleFor(x => x.ComplianceSchemeId).NotEqual(Guid.Empty);
    }
}