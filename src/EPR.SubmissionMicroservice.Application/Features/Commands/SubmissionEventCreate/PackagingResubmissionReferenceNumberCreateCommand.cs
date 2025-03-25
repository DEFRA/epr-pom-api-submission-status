using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class PackagingResubmissionReferenceNumberCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.PackagingResubmissionReferenceNumberCreated;

    public string PackagingResubmissionReferenceNumber { get; set; }
}