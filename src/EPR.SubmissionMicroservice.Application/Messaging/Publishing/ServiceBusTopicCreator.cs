using Azure.Messaging.ServiceBus.Administration;
using EPR.SubmissionMicroservice.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPR.SubmissionMicroservice.Application.Messaging.Publishing;

public class ServiceBusTopicCreator : IServiceBusTopicCreator
{
    private readonly ILogger<ServiceBusTopicCreator> _logger;
    private readonly ServiceBusAdministrationClient _adminClient;
    private readonly ServiceBusOptions _options;

    public ServiceBusTopicCreator(ILogger<ServiceBusTopicCreator> logger,
        ServiceBusAdministrationClient serviceBusAdministrationClient,
        IOptions<ServiceBusOptions> serviceBusOptions)
    {
        _logger = logger;
        _adminClient = serviceBusAdministrationClient;
        _options = serviceBusOptions.Value;
    }

    public async Task ConfigureTopics()
    {
        using (_logger.BeginScope("Configuring service bus topics"))
        {
            var topicExistsResult =
                await _adminClient.TopicExistsAsync(_options.RegistrationSubmittedForFeesCalculationTopicName);

            if (!topicExistsResult.HasValue)
            {
                throw new InvalidOperationException(
                    "Unable to get a result when trying to query for the existence of a topic");
            }

            _logger.LogInformation("Topic {topicName} found: {topicFound}", _options.RegistrationSubmittedForFeesCalculationTopicName, topicExistsResult.Value);

            if (!topicExistsResult.Value)
            {
                _logger.LogInformation("Creating topic {topicName}...", _options.RegistrationSubmittedForFeesCalculationTopicName);
                await _adminClient.CreateTopicAsync(_options.RegistrationSubmittedForFeesCalculationTopicName);
                _logger.LogInformation("Topic {topicName} successfully created", _options.RegistrationSubmittedForFeesCalculationTopicName);
            }
        }
    }
}