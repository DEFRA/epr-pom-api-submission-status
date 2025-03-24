using System.Diagnostics.CodeAnalysis;

namespace EPR.SubmissionMicroservice.API.Contracts.SubmissionEvents.Get;

[ExcludeFromCodeCoverage]
public class RegulatorPoMDecisionSubmissionEventsGetRequest
{
    public DateTime LastSyncTime { get; set; }
}