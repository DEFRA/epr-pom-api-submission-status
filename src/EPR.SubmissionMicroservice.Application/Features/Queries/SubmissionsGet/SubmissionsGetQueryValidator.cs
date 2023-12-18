namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsGet;

using FluentValidation;

public class SubmissionsGetQueryValidator : AbstractValidator<SubmissionsGetQuery>
{
    public SubmissionsGetQueryValidator()
    {
        RuleFor(x => x.OrganisationId).NotEmpty().NotEqual(default(Guid));
    }
}