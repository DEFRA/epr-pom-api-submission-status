using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class PackagingResubmissionFeeViewCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.PackagingResubmissionFeeViewed;

    public bool? IsPackagingResubmissionFeeViewed { get; set; }
}