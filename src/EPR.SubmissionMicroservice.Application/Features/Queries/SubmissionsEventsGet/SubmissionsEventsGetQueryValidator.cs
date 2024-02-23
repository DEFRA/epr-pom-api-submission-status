namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsEventsGet;

using FluentValidation;

public class SubmissionsEventsGetQueryValidator : AbstractValidator<SubmissionsEventsGetQuery>
{
    public SubmissionsEventsGetQueryValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty().NotEqual(Guid.Empty);

        RuleFor(x => x.LastSyncTime).NotEmpty().NotEqual(default(DateTime));
    }
}
