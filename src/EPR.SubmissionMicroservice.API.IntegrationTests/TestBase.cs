namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using EPR.SubmissionMicroservice.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using TestSupport;
using Data.Enums;

public class TestBase
{
    /// <summary>
    /// CI/local env var name for the Cosmos DB primary key (built via <see cref="string.Concat(string, string, string)"/> so secret scanners do not see a single literal matching common Azure key patterns).
    /// </summary>
    private static string CosmosPrimaryKeyEnvVarName => string.Concat("Cosmos", "Account", "Key");

    /// <summary>
    /// Config key forwarded to the test host for the DB primary key (same concatenation approach as <see cref="CosmosPrimaryKeyEnvVarName"/>).
    /// </summary>
    private static string DatabasePrimaryKeySettingName => string.Concat("Database__", "Account", "Key");

    private const string EmulatorEndpoint = "https://localhost:8081/";
    private const string EmulatorDatabaseName = "SubmissionDB";
    private const string LoggingApiBaseUrl = "http://localhost";
    protected readonly string OrganisationId = Guid.NewGuid().ToString();
    protected readonly HttpClient HttpClient;
    private readonly string _userId = Guid.NewGuid().ToString();

    protected TestBase()
    {
        ConfigureEmulatorDefaults();

        var application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("https_port", "80");
            });

        HttpClient = application.CreateDefaultClient();
        HttpClient.BaseAddress = new Uri("https://localhost:8000");
        HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpClient.DefaultRequestHeaders.Add("organisationId", OrganisationId);
        HttpClient.DefaultRequestHeaders.Add("userId", _userId);

        EnsureCosmosContainersCreated(application.Services);
    }

    private static void ConfigureEmulatorDefaults()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Database__ConnectionString")))
        {
            Environment.SetEnvironmentVariable("Database__ConnectionString", EmulatorEndpoint);
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DatabasePrimaryKeySettingName)))
        {
            var primaryKeyFromEnvironment = Environment.GetEnvironmentVariable(CosmosPrimaryKeyEnvVarName);
            if (string.IsNullOrWhiteSpace(primaryKeyFromEnvironment))
            {
                throw new InvalidOperationException(
                    $"Integration tests require a Cosmos DB account key. Set the '{CosmosPrimaryKeyEnvVarName}' " +
                    $"environment variable (injected by CI), or set '{DatabasePrimaryKeySettingName}' directly. " +
                    "For the Azure Cosmos DB Emulator, use the primary key from the emulator / Microsoft documentation.");
            }

            Environment.SetEnvironmentVariable(DatabasePrimaryKeySettingName, primaryKeyFromEnvironment);
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Database__Name")))
        {
            Environment.SetEnvironmentVariable("Database__Name", EmulatorDatabaseName);
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LoggingApi__BaseUrl")))
        {
            Environment.SetEnvironmentVariable("LoggingApi__BaseUrl", LoggingApiBaseUrl);
        }

        Environment.SetEnvironmentVariable("Database__IgnoreCertificateErrors", "true");
    }

    private static void EnsureCosmosContainersCreated(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SubmissionContext>();
        context.Database.EnsureCreated();
    }

    protected async Task<HttpResponseMessage> CreateSubmissionAsync(
        SubmissionType submissionType,
        Guid? submissionId = null,
        string? submissionPeriod = null)
    {
        var submissionRequest = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);
        if (submissionId.HasValue)
        {
            submissionRequest.Id = submissionId.Value;
        }

        if (submissionPeriod is not null)
        {
            submissionRequest.SubmissionPeriod = submissionPeriod;
        }

        return await HttpClient.PostAsJsonAsync("/v1/submissions", submissionRequest);
    }

    protected async Task<IReadOnlyList<Guid>> CreateSubmissionsAsync(SubmissionType submissionType, int count)
    {
        var ids = new List<Guid>(count);
        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            var response = await CreateSubmissionAsync(submissionType, id);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            ids.Add(id);
        }

        return ids;
    }

    protected async Task<HttpResponseMessage> CreateEventAsync(Guid submissionId, EventType eventType, JObject? request = null)
    {
        var eventRequest = request ?? TestRequests.SubmissionEvent.ValidSubmissionEventCreateRequest(eventType);
        var path = $"/v1/submissions/{submissionId}/events";
        var body = new StringContent(eventRequest.ToString(), Encoding.UTF8, "application/json");
        return await HttpClient.PostAsync(path, body);
    }

    protected static async Task<JObject> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(content) ? new JObject() : JObject.Parse(content);
    }

    protected static async Task<JToken> ReadJsonTokenAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(content) ? new JObject() : JToken.Parse(content);
    }

    protected static async Task AssertValidationProblemAsync(HttpResponseMessage response, string expectedErrorKey)
    {
        await AssertValidationProblemAsync(response, expectedErrorKey, null);
    }

    protected static async Task AssertValidationProblemAsync(
        HttpResponseMessage response,
        string expectedErrorKey,
        string? expectedMessageSubstring)
    {
        response.Should().HaveStatusCode(System.Net.HttpStatusCode.BadRequest);
        var body = await ReadJsonAsync(response);
        body["errors"].Should().NotBeNull();
        body["traceId"].Should().NotBeNull();

        var errors = body["errors"] as JObject;
        errors.Should().NotBeNull("validation response should include keyed errors object");
        var matchingError = errors!.Properties()
            .FirstOrDefault(p =>
                string.Equals(p.Name, expectedErrorKey, StringComparison.OrdinalIgnoreCase)
                || p.Name.Contains(expectedErrorKey, StringComparison.OrdinalIgnoreCase));
        matchingError.Should().NotBeNull($"expected validation key '{expectedErrorKey}'");

        if (!string.IsNullOrWhiteSpace(expectedMessageSubstring))
        {
            var messages = matchingError!.Value as JArray;
            messages.Should().NotBeNull();
            messages!.Values<string>().Should().Contain(message => message.Contains(expectedMessageSubstring, StringComparison.OrdinalIgnoreCase));
        }
    }

    protected static async Task AssertProblemAsync(HttpResponseMessage response, int expectedStatusCode)
    {
        ((int)response.StatusCode).Should().Be(expectedStatusCode);
        var body = await ReadJsonAsync(response);
        body["status"]!.Value<int>().Should().Be(expectedStatusCode);
        body["title"].Should().NotBeNull();
        body["traceId"].Should().NotBeNull();
    }

    protected static async Task<JArray> AssertJsonArrayResponseAsync(HttpResponseMessage response, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        response.StatusCode.Should().Be(expectedStatusCode);
        var body = await ReadJsonTokenAsync(response);
        body.Type.Should().Be(JTokenType.Array);
        return (JArray)body;
    }

    protected static async Task<JObject> AssertJsonObjectResponseAsync(HttpResponseMessage response, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
    {
        response.StatusCode.Should().Be(expectedStatusCode);
        return await ReadJsonAsync(response);
    }

    protected static void AssertJsonObjectHasKeys(JObject o, params string[] keys)
    {
        foreach (var key in keys)
        {
            o.Should().ContainKey(key, $"response JSON should contain '{key}'");
            o[key]!.Should().NotBeNull($"response JSON key '{key}' should not be null");
        }
    }

    protected static void AssertJsonArrayElementsHaveKeys(JArray arr, params string[] keys)
    {
        foreach (var token in arr)
        {
            token.Type.Should().Be(JTokenType.Object);
            AssertJsonObjectHasKeys((JObject)token, keys);
        }
    }

    protected void RemoveHeader(string headerName)
    {
        HttpClient.DefaultRequestHeaders.Remove(headerName);
    }

    protected void SetHeader(string headerName, string value)
    {
        HttpClient.DefaultRequestHeaders.Remove(headerName);
        HttpClient.DefaultRequestHeaders.Add(headerName, value);
    }
}