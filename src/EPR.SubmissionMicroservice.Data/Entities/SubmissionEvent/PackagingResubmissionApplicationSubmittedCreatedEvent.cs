using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

public class PackagingResubmissionApplicationSubmittedCreatedEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.PackagingResubmissionApplicationSubmitted;

    public Guid? FileId { get; set; }

    public bool? IsResubmitted { get; set; }

    public string? SubmittedBy { get; set; }

    public DateTime? SubmissionDate { get; set; }

    public string? Comments { get; set; }
}
