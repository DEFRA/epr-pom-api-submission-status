namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Data.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using TestSupport;

[TestClass]
public class SubmissionEventTests : TestBase
{
    private const string SubmissionEventsBasePath = "/v1/submissions/{0}/events";
    private const string SubmissionsBasePath = "/v1/submissions";
    private const string RegulatorRegistrationDecisionBasePath =
        $"{SubmissionsBasePath}/events/get-regulator-registration-decision";

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    [DataRow(SubmissionType.Producer, EventType.AntivirusCheck)]
    [DataRow(SubmissionType.Producer, EventType.CheckSplitter)]
    [DataRow(SubmissionType.Producer, EventType.ProducerValidation)]
    [DataRow(SubmissionType.Registration, EventType.AntivirusCheck)]
    [DataRow(SubmissionType.Registration, EventType.Registration)]
    public async Task CreateEvent_ReturnsCreated_WhenSubmissionExists(SubmissionType submissionType, EventType eventType)
    {
        var submissionRequest = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);
        await HttpClient.PostAsJsonAsync(SubmissionsBasePath, submissionRequest);
        var response = await CreateEventAsync(submissionRequest.Id, eventType);

        response.Should().HaveStatusCode(HttpStatusCode.Created);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task CreateEvent_EmitsQueryableState_WhenProducerValidationEventIsCreated()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Producer, submissionId);

        var createEventResponse = await CreateEventAsync(submissionId, EventType.ProducerValidation);
        createEventResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var queryResponse = await HttpClient.GetAsync($"/v1/submissions/events/events-by-type/{submissionId}?LastSyncTime=2000-01-01T00:00:00Z");
        queryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(queryResponse);
        AssertJsonObjectHasKeys(body, "submittedEvents", "regulatorDecisionEvents", "antivirusCheckEvents");
        body["submittedEvents"]!.Type.Should().Be(JTokenType.Array);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    [DataRow(EventType.AntivirusCheck)]
    [DataRow(EventType.CheckSplitter)]
    [DataRow(EventType.ProducerValidation)]
    [DataRow(EventType.Registration)]
    [DataRow(EventType.AntivirusResult)]
    public async Task CreateEvent_ReturnsValidationProblem_WhenSubmissionDoesNotExist(EventType eventType)
    {
        var response = await CreateEventAsync(Guid.NewGuid(), eventType);
        await AssertValidationProblemAsync(response, "Submission");
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task CreateEvent_ReturnsValidationProblem_WhenTypeIsMissing()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Producer, submissionId);

        var request = JObject.FromObject(new { producerId = "12345" });
        var response = await CreateEventAsync(submissionId, EventType.ProducerValidation, request);

        await AssertValidationProblemAsync(response, "Type");
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task CreateEvent_ReturnsValidationProblem_WhenTypeIsOutOfRange()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Producer, submissionId);

        var invalidRequest = JObject.FromObject(new { type = 9999 });
        var response = await CreateEventAsync(submissionId, EventType.ProducerValidation, invalidRequest);

        await AssertValidationProblemAsync(response, "Type");
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task CreateEvent_ReturnsBadRequest_WhenJsonIsMalformed()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Producer, submissionId);
        var path = string.Format(SubmissionEventsBasePath, submissionId);

        var malformedBody = new StringContent("not-json", Encoding.UTF8, "application/json");
        var response = await HttpClient.PostAsync(path, malformedBody);

        await AssertValidationProblemAsync(response, "request");
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetRegulatorRegistrationDecisionSubmissionEvents_ReturnsOk_WhenRequestIsValid()
    {
        var path = $"{RegulatorRegistrationDecisionBasePath}?LastSyncTime=2024-01-10";
        var response = await HttpClient.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetRegulatorRegistrationDecisionSubmissionEvents_ReturnsBadRequest_WhenLastSyncTimeIsMissing()
    {
        var path = $"{RegulatorRegistrationDecisionBasePath}?LastSyncTime=";
        var response = await HttpClient.GetAsync(path);
        await AssertValidationProblemAsync(response, "LastSyncTime");
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetRegulatorRegistrationDecisionSubmissionEvents_ReturnsBadRequest_WhenLastSyncTimeIsInvalid()
    {
        var path = $"{RegulatorRegistrationDecisionBasePath}?LastSyncTime=not-a-date";
        var response = await HttpClient.GetAsync(path);
        await AssertValidationProblemAsync(response, "LastSyncTime");
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    [DataRow("2024-01-01T00:00:00Z")]
    [DataRow("2024-01-01T00:00:00+01:00")]
    [DataRow("2024-03-31T00:59:59+00:00")]
    public async Task GetRegulatorRegistrationDecisionSubmissionEvents_ReturnsOk_ForTimezoneBoundaryFormats(string lastSyncTime)
    {
        var path = $"{RegulatorRegistrationDecisionBasePath}?LastSyncTime={Uri.EscapeDataString(lastSyncTime)}";
        var response = await HttpClient.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}