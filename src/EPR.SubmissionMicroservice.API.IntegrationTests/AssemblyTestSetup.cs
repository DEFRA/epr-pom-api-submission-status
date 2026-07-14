using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using EPR.SubmissionMicroservice.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EPR.SubmissionMicroservice.Data;
using Microsoft.Extensions.Options;

namespace EPR.SubmissionMicroservice.API.IntegrationTests;

[TestClass]
public static class AssemblyTestSetup
{
    private const string LoggingApiBaseUrl = "http://localhost";
    private const string RegistrationSubmittedForFeesCalculationSubscriptionName = "integration-tests-reg-submitted-for-fees";
    private const string RegistrationSubmittedForRegulatorApprovalSubscriptionName = "integration-tests-reg-submitted-for-approval";
    private static CustomWebApplicationFactory? _factory;
    private static ServiceBusClient _serviceBusClient;

    public static HttpClient CreateClient()
    {
        if (_factory is null)
        {
            throw new InvalidOperationException("Factory not initialized");
        }

        var client = _factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost:8000");
        return client;
    }
    
    public static ServiceBusReceiver RegistrationSubmittedForFeesCalculationServiceBusReceiver { get; private set; }
    public static ServiceBusReceiver RegistrationSubmittedForRegulatorApprovalServiceBusReceiver { get; private set; }

    public static IServiceProvider SharedServices
    {
        get => _factory?.Services ?? throw new InvalidOperationException("SharedServices not initialized");
    }

    [AssemblyInitialize]
    public static async Task AssemblyInitialize(TestContext context)
    {
        // Configure emulator defaults
        ConfigureEmulatorDefaults();

        // Build test configuration
        var testConfig = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();

        // Create factory and client once for the entire test run
        _factory = new CustomWebApplicationFactory(testConfig);

        // Ensure Cosmos containers are created
        await EnsureCosmosContainersCreatedAsync(_factory.Services);

        // Wait for Service Bus emulator to be ready
        context.WriteLine("Waiting for Service Bus emulator to be ready...");
        await WaitForServiceBusEmulatorAsync(context);

        // subscribe to service bus
        var serviceBusAdminClient = SharedServices.GetRequiredService<ServiceBusAdministrationClient>();
        var serviceBusConfig = SharedServices.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
        
        await EnsureTopicExists(context, serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName, RegistrationSubmittedForFeesCalculationSubscriptionName, serviceBusAdminClient);
        await EnsureTopicExists(context, serviceBusConfig.RegistrationSubmittedForRegulatorApprovalTopicName, RegistrationSubmittedForRegulatorApprovalSubscriptionName, serviceBusAdminClient);

        _serviceBusClient = SharedServices.GetRequiredService<ServiceBusClient>();

        var serviceBusReceiveOptions = new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
        };

        context.WriteLine($"Creating service bus receiver for topic {serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName} and subscription {RegistrationSubmittedForFeesCalculationSubscriptionName}");
        RegistrationSubmittedForFeesCalculationServiceBusReceiver = _serviceBusClient.CreateReceiver(
            serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName, RegistrationSubmittedForFeesCalculationSubscriptionName, serviceBusReceiveOptions);

        context.WriteLine($"Creating service bus receiver for topic {serviceBusConfig.RegistrationSubmittedForRegulatorApprovalTopicName} and subscription {RegistrationSubmittedForRegulatorApprovalSubscriptionName}");
        RegistrationSubmittedForRegulatorApprovalServiceBusReceiver = _serviceBusClient.CreateReceiver(
            serviceBusConfig.RegistrationSubmittedForRegulatorApprovalTopicName, RegistrationSubmittedForRegulatorApprovalSubscriptionName, serviceBusReceiveOptions);
    }

    private static async Task EnsureTopicExists(TestContext context, string topicName, string subscriptionName,
        ServiceBusAdministrationClient serviceBusAdminClient)
    {
        context.WriteLine($"Creating service bus subscription for topic {topicName} and subscription {subscriptionName}...");
        
        var topicExistsResult =
            await serviceBusAdminClient.TopicExistsAsync(topicName);

        context.WriteLine("Topic {0} found: {1}", topicName, topicExistsResult.Value);

        if (!topicExistsResult.Value)
        {
            context.WriteLine("Creating topic {0}...", topicName);
            await serviceBusAdminClient.CreateTopicAsync(topicName);
        }

        context.WriteLine("Creating subscription {0}...",
            RegistrationSubmittedForFeesCalculationSubscriptionName);
        await serviceBusAdminClient.CreateSubscriptionAsync(topicName, subscriptionName);
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        var serviceBusAdminClient = SharedServices.GetRequiredService<ServiceBusAdministrationClient>();
        var serviceBusConfig = SharedServices.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
        await serviceBusAdminClient.DeleteSubscriptionAsync(
            serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName, RegistrationSubmittedForFeesCalculationSubscriptionName);
        await serviceBusAdminClient.DeleteSubscriptionAsync(
            serviceBusConfig.RegistrationSubmittedForRegulatorApprovalTopicName, RegistrationSubmittedForRegulatorApprovalSubscriptionName);
        await RegistrationSubmittedForFeesCalculationServiceBusReceiver.DisposeAsync();
        await RegistrationSubmittedForRegulatorApprovalServiceBusReceiver.DisposeAsync();
        await _serviceBusClient.DisposeAsync();

        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }

    private static void ConfigureEmulatorDefaults()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LoggingApi__BaseUrl")))
        {
            Environment.SetEnvironmentVariable("LoggingApi__BaseUrl", LoggingApiBaseUrl);
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Database__IgnoreCertificateErrors")))
        {
            Environment.SetEnvironmentVariable("Database__IgnoreCertificateErrors", "true");
        }
    }

    private static async Task WaitForServiceBusEmulatorAsync(TestContext context)
    {
        const int maxRetries = 60;
        const int delayMs = 1000; // 1 second between retries
        int attempt = 0;

        // Allow self-signed certificates for the local emulator
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        using var httpClient = new HttpClient(handler);
        httpClient.Timeout = TimeSpan.FromSeconds(3);

        while (attempt < maxRetries)
        {
            attempt++;
            try
            {
                // Try to make a simple request to the Service Bus emulator admin endpoint
                using var response = await httpClient.GetAsync("http://localhost:5300/");
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // If we get any response (even 401), the endpoint is up
                    context.WriteLine("Service Bus emulator is ready");
                    // Additional delay to ensure the emulator is fully initialized
                    await Task.Delay(2000);
                    return;
                }
            }
            catch (TaskCanceledException)
            {
                context.WriteLine($"Service Bus emulator not ready yet (attempt {attempt}/{maxRetries}): Timeout");
                if (attempt < maxRetries)
                {
                    await Task.Delay(delayMs);
                }
                else
                {
                    throw new InvalidOperationException($"Service Bus emulator failed to start after {maxRetries * delayMs / 1000} seconds", new TimeoutException("Connection timeout"));
                }
            }
            catch (HttpRequestException ex)
            {
                context.WriteLine($"Service Bus emulator not ready yet (attempt {attempt}/{maxRetries}): {ex.Message}");
                if (attempt < maxRetries)
                {
                    await Task.Delay(delayMs);
                }
                else
                {
                    throw new InvalidOperationException($"Service Bus emulator failed to start after {maxRetries * delayMs / 1000} seconds", ex);
                }
            }
            catch (Exception ex)
            {
                context.WriteLine($"Service Bus emulator not ready yet (attempt {attempt}/{maxRetries}): {ex.GetType().Name}: {ex.Message}");
                if (attempt < maxRetries)
                {
                    await Task.Delay(delayMs);
                }
                else
                {
                    throw new InvalidOperationException($"Service Bus emulator failed to start after {maxRetries * delayMs / 1000} seconds", ex);
                }
            }
        }

        throw new InvalidOperationException("Service Bus emulator failed to start");
    }

    private static async Task EnsureCosmosContainersCreatedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SubmissionContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
