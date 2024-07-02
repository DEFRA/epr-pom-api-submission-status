using System.Diagnostics.CodeAnalysis;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
[ExcludeFromCodeCoverage]
public class RegulatorPoMDecisionSubmissionEventGetQueryValidator : AbstractValidator<RegulatorDecisionSubmissionEventGetQuery>
{
    public RegulatorPoMDecisionSubmissionEventGetQueryValidator()
    {
        RuleFor(x => x.LastSyncTime).NotEmpty();
    }
}