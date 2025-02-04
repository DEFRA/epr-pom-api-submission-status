using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

[ExcludeFromCodeCoverage]
public class PackagingResubmissionApplicationSubmittedEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.PackagingResubmissionApplicationSubmitted;

    public string? Comments { get; set; }

    public string? ApplicationReferenceNumber { get; set; }

    public DateTime? SubmissionDate { get; set; }
}