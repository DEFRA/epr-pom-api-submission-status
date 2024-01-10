using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorRegistrationDecisionSubmissionEventsGetQueryValidator : AbstractValidator<RegulatorRegistrationDecisionSubmissionEventsGetQuery>
{
    public RegulatorRegistrationDecisionSubmissionEventsGetQueryValidator()
    {
        RuleFor(x => x.LastSyncTime).NotEmpty();
    }
}