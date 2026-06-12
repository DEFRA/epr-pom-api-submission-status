using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EPR.SubmissionMicroservice.Application.Logging;
using MediatR;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace EPR.SubmissionMicroservice.Application.Messaging.Publishing.RegistrationSubmittedForFeesCalculation;

public class RegistrationSubmittedForFeesCalculationNotificationHandler : INotificationHandler<RegistrationSubmittedForFeesCalculationNotification>
{
    private readonly ILogger<RegistrationSubmittedForFeesCalculationNotificationHandler> _logger;
    private readonly ServiceBusSender _serviceBusSender;

    public RegistrationSubmittedForFeesCalculationNotificationHandler(ILogger<RegistrationSubmittedForFeesCalculationNotificationHandler> logger, IAzureClientFactory<ServiceBusSender> senderFactory)
    {
        _logger = logger;
        _serviceBusSender = senderFactory.CreateClient(nameof(RegistrationSubmittedForFeesCalculationNotification));
    }
    
    public async Task Handle(RegistrationSubmittedForFeesCalculationNotification notification, CancellationToken cancellationToken)
    {
        using (_logger.AddScopedData(new Dictionary<string, object>
               {
                   ["SubmissionId"] = notification.SubmissionId,
                   ["RegistrationBlobName"] = notification.RegistrationBlobName,
                   ["ComplianceSchemeId"] = notification.ComplianceSchemeId,
                   ["SubmissionPeriod"] = notification.SubmissionPeriod,
                   ["SubmissionDate"] = notification.SubmissionDate,
               }))
        {
            string messagePayload = JsonSerializer.Serialize(notification);
            var message = new ServiceBusMessage(messagePayload);

            try
            {
                _logger.LogInformation("Publishing message to message bus...");
                await _serviceBusSender.SendMessageAsync(message, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to publish message");
            }

        }
    }
}