using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class PackagingResubmissionApplicationSubmittedCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.PackagingResubmissionApplicationSubmitted;

    public Guid? FileId { get; set; }

    public bool? IsResubmitted { get; set; }

    public string? SubmittedBy { get; set; }

    public DateTime? SubmissionDate { get; set; }

    public string? Comments { get; set; }
}
