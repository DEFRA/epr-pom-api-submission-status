namespace EPR.SubmissionMicroservice.Application.Features.Notifications.SubmissionEventNotification;

using MediatR;

public record SubmissionEventNotification(Guid Id) : INotification;