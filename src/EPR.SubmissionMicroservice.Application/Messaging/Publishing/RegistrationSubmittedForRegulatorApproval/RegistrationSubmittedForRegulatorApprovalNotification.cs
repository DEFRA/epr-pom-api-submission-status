using MediatR;

namespace EPR.SubmissionMicroservice.Application.Messaging.Publishing.RegistrationSubmittedForRegulatorApproval;

public record RegistrationSubmittedForRegulatorApprovalNotification(
    Guid SubmissionId,
    string ApplicationReferenceNumber,
    DateTime SubmissionDate) : INotification;
