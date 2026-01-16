using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class RegistrationFeePaymentEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.RegistrationFeePayment;

    public string? ApplicationReferenceNumber { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public string PaymentMethod { get; set; }

    public string PaymentStatus { get; set; }

    public string PaidAmount { get; set; }

    public bool? IsResubmission { get; set; }

    public string? RegistrationJourney { get; set; }
}