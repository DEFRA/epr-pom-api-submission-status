using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryValidator : AbstractValidator<RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery>
{
    public RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryValidator()
    {
        RuleFor(x => x.LastSyncTime).NotEmpty();
    }
}
