namespace EPR.SubmissionMicroservice.Application.Features.Notifications.SubmissionEventNotification;

using MediatR;
using Microsoft.Extensions.Logging;

public class SubmissionEventNotificationHandler : INotificationHandler<SubmissionEventNotification>
{
    private readonly ILogger<SubmissionEventNotificationHandler> _logger;

    public SubmissionEventNotificationHandler(ILogger<SubmissionEventNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(SubmissionEventNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogCritical("SubmissionEventNotificationHandler: {Id}", notification.Id);
    }
}