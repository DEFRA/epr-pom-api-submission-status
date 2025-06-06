using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class PackagingResubmissionFeeViewCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.PackagingResubmissionFeeViewed;

    public Guid? FileId { get; set; }

    public bool? IsPackagingResubmissionFeeViewed { get; set; }
}