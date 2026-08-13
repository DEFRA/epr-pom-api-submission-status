using MediatR;

namespace EPR.SubmissionMicroservice.Application.Messaging.Publishing.RegistrationSubmittedForFeesCalculation;

public record RegistrationSubmittedForFeesCalculationNotification(
    Guid SubmissionId,
    string RegistrationBlobName,
    Guid? ComplianceSchemeId,
    DateTime SubmissionDate,
    int? SubmissionPeriodId,
    string RegulatorNation,
    string ApplicationReferenceNumber) : INotification;
