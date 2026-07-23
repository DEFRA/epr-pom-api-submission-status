using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EPR.SubmissionMicroservice.Application.Logging;
using MediatR;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace EPR.SubmissionMicroservice.Application.Messaging.Publishing.RegulatorRegistrationDecision;

public class RegulatorRegistrationDecisionNotificationHandler : INotificationHandler<RegulatorRegistrationDecisionNotification>
{
    private readonly ILogger<RegulatorRegistrationDecisionNotificationHandler> _logger;
    private readonly ServiceBusSender _serviceBusSender;

    public RegulatorRegistrationDecisionNotificationHandler(
        ILogger<RegulatorRegistrationDecisionNotificationHandler> logger,
        IAzureClientFactory<ServiceBusSender> senderFactory)
    {
        _logger = logger;
        _serviceBusSender = senderFactory.CreateClient(nameof(RegulatorRegistrationDecisionNotification));
    }

    public async Task Handle(RegulatorRegistrationDecisionNotification notification, CancellationToken cancellationToken)
    {
        using (_logger.AddScopedData(new Dictionary<string, object>
               {
                   ["SubmissionId"] = notification.SubmissionId,
                   ["EventName"] = notification.EventName,
                   ["DecisionDate"] = notification.DecisionDate,
               }))
        {
            var messagePayload = JsonSerializer.Serialize(notification);
            var message = new ServiceBusMessage(messagePayload);

            _logger.LogInformation("Publishing message to message bus...");
            await _serviceBusSender.SendMessageAsync(message, cancellationToken);
            _logger.LogInformation("Message published");
        }
    }
}
