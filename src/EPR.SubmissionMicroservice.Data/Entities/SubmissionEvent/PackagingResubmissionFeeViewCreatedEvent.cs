using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

public class PackagingResubmissionFeeViewCreatedEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.PackagingResubmissionFeeViewed;

    public bool? IsPackagingResubmissionFeeViewed { get; set; }
}
