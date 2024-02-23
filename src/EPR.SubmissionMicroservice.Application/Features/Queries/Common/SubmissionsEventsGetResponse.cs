namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubmissionsEventsGetResponse
{
    public SubmissionsEventsGetResponse()
    {
        SubmittedEvents = new List<SubmittedEvents>();
        RegulatorDecisionEvents = new List<RegulatorDecisionEvents>();
        AntivirusCheckEvents = new List<AntivirusCheckEvents>();
    }

    public List<SubmittedEvents> SubmittedEvents { get; set; }

    public List<RegulatorDecisionEvents> RegulatorDecisionEvents { get; set; }

    public List<AntivirusCheckEvents> AntivirusCheckEvents { get; set; }
}