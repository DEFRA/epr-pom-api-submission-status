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
    private const string IntegrationTestsSubscriptionName = "integration-tests";

    // Readiness endpoint exposed by the Service Bus emulator on EMULATOR_HTTP_PORT (default 5300).
    private const string ServiceBusEmulatorHealthUrl = "http://localhost:5300/health";
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

    public static ServiceBusReceiver RegulatorRegistrationDecisionServiceBusReceiver { get; private set; }

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

        var topicExistsResult =
            await serviceBusAdminClient.TopicExistsAsync(serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName);

        var serviceBusReceiveOptions = new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
        };

        RegistrationSubmittedForFeesCalculationServiceBusReceiver = await SetupTopicSubscriptionAndReceiverAsync(
            context,
            serviceBusAdminClient,
            serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName,
            serviceBusReceiveOptions);

        RegistrationSubmittedForRegulatorApprovalServiceBusReceiver = await SetupTopicSubscriptionAndReceiverAsync(
            context,
            serviceBusAdminClient,
            serviceBusConfig.RegistrationSubmittedForRegulatorApprovalTopicName,
            serviceBusReceiveOptions);

        RegulatorRegistrationDecisionServiceBusReceiver = await SetupTopicSubscriptionAndReceiverAsync(
            context,
            serviceBusAdminClient,
            serviceBusConfig.RegulatorRegistrationDecisionTopicName,
            serviceBusReceiveOptions);
    }

    private static async Task<ServiceBusReceiver> SetupTopicSubscriptionAndReceiverAsync(
        TestContext context,
        ServiceBusAdministrationClient serviceBusAdminClient,
        string topicName,
        ServiceBusReceiverOptions serviceBusReceiveOptions)
    {
        context.WriteLine($"Creating service bus subscription for topic {topicName} and subscription {IntegrationTestsSubscriptionName}...");

        var topicExistsResult = await WithEmulatorWarmUpRetryAsync(
            context,
            "Checking whether the topic exists",
            () => serviceBusAdminClient.TopicExistsAsync(topicName));

        context.WriteLine("Topic {0} found: {1}", topicName, topicExistsResult.Value);

        if (!topicExistsResult.Value)
        {
            context.WriteLine("Creating topic {0}...", topicName);
            await WithEmulatorWarmUpRetryAsync(
                context,
                "Creating the topic",
                () => serviceBusAdminClient.CreateTopicAsync(topicName));
        }

        // A previous run whose cleanup did not complete can leave the subscription behind, which
        // would make CreateSubscriptionAsync fail with MessagingEntityAlreadyExists.
        var subscriptionExistsResult = await WithEmulatorWarmUpRetryAsync(
            context,
            "Checking whether the subscription exists",
            () => serviceBusAdminClient.SubscriptionExistsAsync(topicName, IntegrationTestsSubscriptionName));

        if (subscriptionExistsResult.Value)
        {
            context.WriteLine("Deleting stale subscription {0} on topic {1}...", IntegrationTestsSubscriptionName, topicName);
            await WithEmulatorWarmUpRetryAsync(
                context,
                "Deleting the stale subscription",
                () => serviceBusAdminClient.DeleteSubscriptionAsync(topicName, IntegrationTestsSubscriptionName));
        }

        context.WriteLine("Creating subscription {0} on topic {1}...", IntegrationTestsSubscriptionName, topicName);
        await WithEmulatorWarmUpRetryAsync(
            context,
            "Creating the subscription",
            () => serviceBusAdminClient.CreateSubscriptionAsync(topicName, IntegrationTestsSubscriptionName));

        context.WriteLine($"Creating service bus receiver for topic {topicName} and subscription {IntegrationTestsSubscriptionName}");
        return _serviceBusClient.CreateReceiver(topicName, IntegrationTestsSubscriptionName, serviceBusReceiveOptions);
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        var serviceBusAdminClient = SharedServices.GetRequiredService<ServiceBusAdministrationClient>();
        var serviceBusConfig = SharedServices.GetRequiredService<IOptions<ServiceBusOptions>>().Value;

        await DeleteSubscriptionIfExistsAsync(serviceBusAdminClient, serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName);
        await DeleteSubscriptionIfExistsAsync(serviceBusAdminClient, serviceBusConfig.RegistrationSubmittedForRegulatorApprovalTopicName);
        await DeleteSubscriptionIfExistsAsync(serviceBusAdminClient, serviceBusConfig.RegulatorRegistrationDecisionTopicName);

        await RegistrationSubmittedForFeesCalculationServiceBusReceiver.DisposeAsync();
        await RegistrationSubmittedForRegulatorApprovalServiceBusReceiver.DisposeAsync();
        await RegulatorRegistrationDecisionServiceBusReceiver.DisposeAsync();
        await _serviceBusClient.DisposeAsync();

        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }

    private static async Task DeleteSubscriptionIfExistsAsync(ServiceBusAdministrationClient serviceBusAdminClient, string topicName)
    {
        await WithEmulatorWarmUpRetryAsync(
            context: null,
            $"Deleting the subscription on {topicName}",
            () => serviceBusAdminClient.DeleteSubscriptionAsync(topicName, IntegrationTestsSubscriptionName));
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

    /// <summary>
    /// Runs a Service Bus administration operation, retrying while the emulator's gateway still
    /// answers "Service is warming up" (HTTP 503 / <see cref="ServiceBusFailureReason.ServiceBusy"/>).
    /// The HTTP readiness probe can succeed slightly before the gateway accepts admin operations,
    /// so these calls need their own tolerance for the warm-up window.
    /// </summary>
    private static async Task<T> WithEmulatorWarmUpRetryAsync<T>(
        TestContext? context,
        string description,
        Func<Task<T>> operation)
    {
        const int maxAttempts = 60;
        const int delayMs = 1000;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (ServiceBusException ex)
                when (ex.Reason == ServiceBusFailureReason.ServiceBusy && attempt < maxAttempts)
            {
                var message = $"{description} failed because the emulator is still warming up (attempt {attempt}/{maxAttempts}); retrying...";
                if (context is null)
                {
                    Console.WriteLine(message);
                }
                else
                {
                    context.WriteLine(message);
                }

                await Task.Delay(delayMs);
            }
        }

        throw new InvalidOperationException(
            $"{description} did not succeed within {maxAttempts * delayMs / 1000} seconds.");
    }

    private static async Task WaitForServiceBusEmulatorAsync(TestContext context)
    {
        const int maxAttempts = 180;
        const int delayMs = 1000; // 1 second between retries

        // Allow self-signed certificates for the local emulator
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        using var httpClient = new HttpClient(handler);
        httpClient.Timeout = TimeSpan.FromSeconds(3);

        var lastFailure = "no attempt made";
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync(ServiceBusEmulatorHealthUrl);

                // The emulator serves 503 on every endpoint until it has finished waiting for SQL
                // and provisioning its databases, so only a successful (or authentication
                // challenged) response means it is ready to serve.
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    context.WriteLine($"Service Bus emulator is ready (attempt {attempt}/{maxAttempts})");
                    // Additional delay to ensure the emulator is fully initialized
                    await Task.Delay(2000);
                    return;
                }

                lastException = null;
                lastFailure = $"HTTP {(int)response.StatusCode} {response.StatusCode}";
            }
            catch (Exception ex)
            {
                lastException = ex;
                lastFailure = $"{ex.GetType().Name}: {ex.Message}";
            }

            context.WriteLine($"Service Bus emulator not ready yet (attempt {attempt}/{maxAttempts}): {lastFailure}");
            await Task.Delay(delayMs);
        }

        throw new InvalidOperationException(
            $"Service Bus emulator failed to start after {maxAttempts * delayMs / 1000} seconds. Last failure: {lastFailure}",
            lastException);
    }

    private static async Task EnsureCosmosContainersCreatedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SubmissionContext>();
        await context.Database.EnsureCreatedAsync();
    }
}