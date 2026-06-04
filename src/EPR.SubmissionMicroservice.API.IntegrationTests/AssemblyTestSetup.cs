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
    private static CustomWebApplicationFactory? _factory;
    private static HttpClient? _sharedHttpClient;
    private static ServiceBusClient _serviceBusClient;

    public static HttpClient SharedHttpClient
    {
        get => _sharedHttpClient ?? throw new InvalidOperationException("SharedHttpClient not initialized");
    }
    
    public static ServiceBusReceiver ServiceBusReceiver { get; private set; }

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
        _sharedHttpClient = _factory.CreateClient();
        _sharedHttpClient.BaseAddress = new Uri("https://localhost:8000");

        // Ensure Cosmos containers are created
        EnsureCosmosContainersCreated(_factory.Services);
        
        // Wait for Service Bus emulator to be ready
        context.WriteLine("Waiting for Service Bus emulator to be ready...");
        await WaitForServiceBusEmulatorAsync(context);
        
        // subscribe to service bus
        var serviceBusAdminClient = SharedServices.GetRequiredService<ServiceBusAdministrationClient>();
        var serviceBusConfig = SharedServices.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
        context.WriteLine($"Creating service bus subscription for topic {serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName} and subscription {IntegrationTestsSubscriptionName}...");
        
        var topicExistsResult =
            await serviceBusAdminClient.TopicExistsAsync(serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName);

        context.WriteLine("Topic {0} found: {1}", serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName, topicExistsResult.Value);

        if (!topicExistsResult.Value)
        {
            context.WriteLine("Creating topic {0}...",
                serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName);
            await serviceBusAdminClient.CreateTopicAsync(serviceBusConfig
                .RegistrationSubmittedForFeesCalculationTopicName);
        }

        context.WriteLine("Creating subscription {0}...",
            IntegrationTestsSubscriptionName);
        await serviceBusAdminClient.CreateSubscriptionAsync(
            serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName, IntegrationTestsSubscriptionName);
        
        _serviceBusClient = SharedServices.GetRequiredService<ServiceBusClient>();

        var serviceBusReceiveOptions = new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
        };
        
        context.WriteLine($"Creating service bus receiver for topic {serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName} and subscription {IntegrationTestsSubscriptionName}");
        ServiceBusReceiver = _serviceBusClient.CreateReceiver(
            serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName, IntegrationTestsSubscriptionName, serviceBusReceiveOptions);
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        var serviceBusAdminClient = SharedServices.GetRequiredService<ServiceBusAdministrationClient>();
        var serviceBusConfig = SharedServices.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
        await serviceBusAdminClient.DeleteSubscriptionAsync(
            serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName, IntegrationTestsSubscriptionName);
        _sharedHttpClient?.Dispose();
        await ServiceBusReceiver.DisposeAsync();
        await _serviceBusClient.DisposeAsync();
        _factory?.DisposeAsync();
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

    private static void EnsureCosmosContainersCreated(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SubmissionContext>();
        context.Database.EnsureCreated();
    }
}
