using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorPoMDecisionSubmissionEventsGetQueryValidator : AbstractValidator<RegulatorPoMDecisionSubmissionEventsGetQuery>
{
    public RegulatorPoMDecisionSubmissionEventsGetQueryValidator()
    {
        RuleFor(x => x.LastSyncTime).NotEmpty();
    }
}