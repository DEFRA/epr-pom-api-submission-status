using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class RegistrationApplicationSubmittedEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.RegistrationApplicationSubmitted;

    public string? Comments { get; set; }

    public string? ApplicationReferenceNumber { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public DateTime? SubmissionDate { get; set; }

    public bool? IsResubmission { get; set; }
}