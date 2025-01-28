using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class PackagingDataResubmissionFeePaymentEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.PackagingDataResubmissionFeePayment;

    public string? ReferenceNumber { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public string PaymentMethod { get; set; }

    public string PaymentStatus { get; set; }

    public string PaidAmount { get; set; }
}