using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

[ExcludeFromCodeCoverage]
public class RegistrationFeePaymentEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.RegistrationFeePayment;

    public string? ApplicationReferenceNumber { get; set; }

    public string PaymentMethod { get; set; } = "";

    public string PaymentStatus { get; set; }

    public string PaidAmount { get; set; }

    public bool? IsResubmission { get; set; }

    public string? RegistrationJourney { get; set; }
}