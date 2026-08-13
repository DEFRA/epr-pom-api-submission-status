using Azure.Messaging.ServiceBus.Administration;
using EPR.SubmissionMicroservice.Application.Logging;
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
            // in a world where we don't need the topic names in appsettings (because we aren't sharing service buses between environments)
            // the list of publisher classes can be injected into the constructor, and we can loop through them creating topics
            await EnsureTopicExists(_options.RegistrationSubmittedForFeesCalculationTopicName);
            await EnsureTopicExists(_options.RegistrationSubmittedForRegulatorApprovalTopicName);
            await EnsureTopicExists(_options.RegulatorRegistrationDecisionTopicName);
        }
    }

    private async Task EnsureTopicExists(string topicName)
    {
        using (_logger.AddScopedData(new Dictionary<string, object>
               {
                   ["TopicName"] = topicName
               }))
        {
            _logger.LogInformation("Checking if topic exists");
            var topicExistsResult =
                await _adminClient.TopicExistsAsync(topicName);

            if (!topicExistsResult.HasValue)
            {
                const string errorMessage = "Unable to get a result when trying to query for the existence of a topic";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation("Topic found: {topicFound}", topicExistsResult.Value);

            if (!topicExistsResult.Value)
            {
                _logger.LogInformation("Creating topic");
                await _adminClient.CreateTopicAsync(topicName);
                _logger.LogInformation("Topic successfully created");
            }
        }
    }
}