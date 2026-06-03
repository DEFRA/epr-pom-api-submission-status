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
        
        // subscribe to service bus
        var serviceBusAdminClient = SharedServices.GetRequiredService<ServiceBusAdministrationClient>();
        var serviceBusConfig = SharedServices.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
        await serviceBusAdminClient.CreateSubscriptionAsync(
            serviceBusConfig.RegistrationSubmittedForFeesCalculationTopicName, IntegrationTestsSubscriptionName);
        
        var serviceBusClient = SharedServices.GetRequiredService<ServiceBusClient>();

        var serviceBusReceiveOptions = new ServiceBusReceiverOptions();
        serviceBusReceiveOptions.ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete;
        ServiceBusReceiver = serviceBusClient.CreateReceiver(
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
        _factory?.Dispose();
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

    private static void EnsureCosmosContainersCreated(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SubmissionContext>();
        context.Database.EnsureCreated();
    }
}
