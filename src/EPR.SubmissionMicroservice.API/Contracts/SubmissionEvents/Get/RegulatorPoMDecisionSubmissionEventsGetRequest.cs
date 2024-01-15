using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.API.Contracts.SubmissionEvents.Get;

public class RegulatorPoMDecisionSubmissionEventsGetRequest
{
    public DateTime LastSyncTime { get; set; }
}