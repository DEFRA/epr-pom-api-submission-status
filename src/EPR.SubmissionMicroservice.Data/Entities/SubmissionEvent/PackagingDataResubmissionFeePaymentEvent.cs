using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

[ExcludeFromCodeCoverage]
public class PackagingDataResubmissionFeePaymentEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.PackagingDataResubmissionFeePayment;

    public string? ReferenceNumber { get; set; }

    public string PaymentMethod { get; set; }

    public string PaymentStatus { get; set; }

    public string PaidAmount { get; set; }
}