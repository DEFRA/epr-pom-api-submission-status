using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EPR.SubmissionMicroservice.Data;

namespace EPR.SubmissionMicroservice.API.IntegrationTests;

[TestClass]
public static class AssemblyTestSetup
{
    private const string LoggingApiBaseUrl = "http://localhost";
    private static CustomWebApplicationFactory? _factory;
    private static HttpClient? _sharedHttpClient;

    public static HttpClient SharedHttpClient
    {
        get => _sharedHttpClient ?? throw new InvalidOperationException("SharedHttpClient not initialized");
    }

    public static IServiceProvider SharedServices
    {
        get => _factory?.Services ?? throw new InvalidOperationException("SharedServices not initialized");
    }

    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext context)
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
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
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
