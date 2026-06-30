namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using TestSupport;
using Data.Enums;

public class TestBase
{
    protected readonly string OrganisationId = Guid.NewGuid().ToString();
    protected readonly HttpClient HttpClient;
    private readonly string _userId = Guid.NewGuid().ToString();

    protected TestBase()
    {
        // Use the shared HttpClient from AssemblyTestSetup (initialized once per test run)
        HttpClient = AssemblyTestSetup.CreateClient();

        // Ensure Accept header is set (idempotent operation)
        if (!HttpClient.DefaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
        {
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // Set per-test headers
        SetHeader("organisationId", OrganisationId);
        SetHeader("userId", _userId);
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

    protected static async Task<bool> HasMessageBeenPublished<T>()
    {
        var message = await AssemblyTestSetup.ServiceBusReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1));

        if (message == null) return false;
        
        var typedMessage = message.Body.ToObjectFromJson<T>();
        
        return typedMessage != null;
    }

    protected static async Task<T> GetPublishedMessage<T>()
    {
        var message = await AssemblyTestSetup.ServiceBusReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1));
        Assert.IsNotNull(message, "message should not be null");
        Assert.IsNotNull(message.Body, "body should not be null");
        var typedMessage = message.Body.ToObjectFromJson<T>();
        Assert.IsNotNull(typedMessage, "cannot convert message to expected type");
        return typedMessage;
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        // purge is in preview, so unavailable
        await AssemblyTestSetup.ServiceBusReceiver.ReceiveMessagesAsync(100, TimeSpan.FromSeconds(1));
    }
}