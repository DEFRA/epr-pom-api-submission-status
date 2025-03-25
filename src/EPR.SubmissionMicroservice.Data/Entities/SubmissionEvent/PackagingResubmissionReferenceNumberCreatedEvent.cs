using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

public class PackagingResubmissionReferenceNumberCreatedEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.PackagingResubmissionReferenceNumberCreated;

    public string PackagingResubmissionReferenceNumber { get; set; }
}