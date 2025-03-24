using System.Diagnostics.CodeAnalysis;

namespace EPR.SubmissionMicroservice.API.Contracts.SubmissionEvents.Get;

[ExcludeFromCodeCoverage]
public class RegulatorOrganisationRegistrationDecisionSubmissionEventsGetRequest
{
    public DateTime LastSyncTime { get; set; }

    public Guid? SubmissionId { get; set; }
}
