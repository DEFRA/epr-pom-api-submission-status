using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

[ExcludeFromCodeCoverage]
public class RegistrationApplicationSubmittedEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.RegistrationApplicationSubmitted;

    public string? Comments { get; set; }

    public string? ApplicationReferenceNumber { get; set; }

    public DateTime? SubmissionDate { get; set; }

    public bool? IsResubmission { get; set; }
}