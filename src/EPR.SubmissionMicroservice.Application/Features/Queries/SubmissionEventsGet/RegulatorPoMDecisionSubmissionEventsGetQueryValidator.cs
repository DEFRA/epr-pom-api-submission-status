﻿using System.Diagnostics.CodeAnalysis;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

[ExcludeFromCodeCoverage]
public class RegulatorPoMDecisionSubmissionEventsGetQueryValidator : AbstractValidator<RegulatorPoMDecisionSubmissionEventsGetQuery>
{
    public RegulatorPoMDecisionSubmissionEventsGetQueryValidator()
    {
        RuleFor(x => x.LastSyncTime).NotEmpty();
    }
}