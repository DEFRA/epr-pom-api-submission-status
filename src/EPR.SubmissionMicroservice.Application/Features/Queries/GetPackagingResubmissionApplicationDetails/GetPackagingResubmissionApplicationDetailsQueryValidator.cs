using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;

public class GetPackagingResubmissionApplicationDetailsQueryValidator : AbstractValidator<GetPackagingResubmissionApplicationDetailsQuery>
{
    public GetPackagingResubmissionApplicationDetailsQueryValidator()
    {
        RuleFor(x => x.OrganisationId).NotEmpty().NotEqual(Guid.Empty);

        RuleFor(x => x.SubmissionPeriod).NotEmpty().WithMessage("Please enter a valid submission period");
    }
}