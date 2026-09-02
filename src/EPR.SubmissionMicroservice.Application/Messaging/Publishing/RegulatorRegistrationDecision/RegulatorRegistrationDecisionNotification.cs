using MediatR;

namespace EPR.SubmissionMicroservice.Application.Messaging.Publishing.RegulatorRegistrationDecision;

public record RegulatorRegistrationDecisionNotification(
    Guid SubmissionId,
    string EventName,
    DateTime DecisionDate) : INotification;
