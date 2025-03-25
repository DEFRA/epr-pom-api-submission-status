using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

public class RegulatorPoMDecisionEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.RegulatorPoMDecision;

    public RegulatorDecision Decision { get; set; }

    public string Comments { get; set; }

    public bool IsResubmissionRequired { get; set; }

    public Guid FileId { get; set; }

    public string RegistrationReferenceNumber { get; set; }
}