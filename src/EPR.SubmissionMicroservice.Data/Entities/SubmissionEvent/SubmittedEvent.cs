namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

using Enums;

public class SubmittedEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.Submitted;

    public Guid FileId { get; set; }

    public string? SubmittedBy { get; set; }

    public bool? IsResubmission { get; set; }
}