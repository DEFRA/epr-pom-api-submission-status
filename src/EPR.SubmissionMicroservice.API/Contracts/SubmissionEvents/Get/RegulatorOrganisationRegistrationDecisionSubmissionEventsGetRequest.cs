namespace EPR.SubmissionMicroservice.API.Contracts.SubmissionEvents.Get;

public class RegulatorOrganisationRegistrationDecisionSubmissionEventsGetRequest
{
    public DateTime LastSyncTime { get; set; }

    public Guid? SubmissionId { get; set; }
}
