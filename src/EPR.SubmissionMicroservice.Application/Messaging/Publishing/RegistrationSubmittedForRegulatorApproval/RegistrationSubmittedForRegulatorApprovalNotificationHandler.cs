using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EPR.SubmissionMicroservice.Application.Logging;
using MediatR;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace EPR.SubmissionMicroservice.Application.Messaging.Publishing.RegistrationSubmittedForRegulatorApproval;

public class RegistrationSubmittedForRegulatorApprovalNotificationHandler : INotificationHandler<RegistrationSubmittedForRegulatorApprovalNotification>
{
    private readonly ILogger<RegistrationSubmittedForRegulatorApprovalNotificationHandler> _logger;
    private readonly ServiceBusSender _serviceBusSender;

    public RegistrationSubmittedForRegulatorApprovalNotificationHandler(ILogger<RegistrationSubmittedForRegulatorApprovalNotificationHandler> logger, IAzureClientFactory<ServiceBusSender> senderFactory)
    {
        _logger = logger;
        _serviceBusSender = senderFactory.CreateClient(nameof(RegistrationSubmittedForRegulatorApprovalNotification));
    }
    
    public async Task Handle(RegistrationSubmittedForRegulatorApprovalNotification notification, CancellationToken cancellationToken)
    {
        using (_logger.AddScopedData(new Dictionary<string, object>
               {
                   ["SubmissionId"] = notification.SubmissionId,
                   ["ApplicationReferenceNumber"] = notification.ApplicationReferenceNumber,
                   ["SubmissionDate"] = notification.SubmissionDate,
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