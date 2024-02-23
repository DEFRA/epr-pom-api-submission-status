namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class RegulatorDecisionEvents : BaseEvent
{
    public string Decision { get; set; }

    public string Comment { get; set; }
}
