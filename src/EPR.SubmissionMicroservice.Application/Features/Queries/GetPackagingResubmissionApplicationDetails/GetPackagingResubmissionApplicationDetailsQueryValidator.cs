using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;

public class GetPackagingResubmissionApplicationDetailsQueryValidator : AbstractValidator<GetPackagingResubmissionApplicationDetailsQuery>
{
    public GetPackagingResubmissionApplicationDetailsQueryValidator()
    {
        RuleFor(x => x.OrganisationId).NotEmpty().NotEqual(Guid.Empty);

        RuleFor(x => x.SubmissionPeriods).NotEmpty().WithMessage("Please enter at least one valid submission period");
    }
}