namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Data.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using TestSupport;

[TestClass]
public class DecisionIntegrationTests : TestBase
{
    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetDecisions_ReturnsOk_WithJsonArray_WhenQueryIsValid()
    {
        var submissionId = Guid.NewGuid();
        var response = await HttpClient.GetAsync(
            $"/v1/decisions?LastSyncTime=2000-01-01T00:00:00Z&SubmissionId={submissionId}&Type=Producer");

        var body = await AssertJsonArrayResponseAsync(response);
        body.Should().NotBeNull();
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetDecisions_ReturnsEntries_WithExpectedKeys_WhenRegistrationSubmittedAndDecisionEventExists()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Registration, submissionId);

        var fileId = Guid.NewGuid();
        var blobName = $"integration-decisions-{Guid.NewGuid():N}.csv";

        var antivirusCheck = JObject.FromObject(new
        {
            type = EventType.AntivirusCheck,
            fileId,
            fileType = FileType.CompanyDetails,
            fileName = "company.csv",
        });
        var antivirusResult = JObject.FromObject(new
        {
            type = EventType.AntivirusResult,
            fileId,
            antivirusScanResult = AntivirusScanResult.Success,
            antivirusScanTrigger = AntivirusScanTrigger.Upload,
            blobName,
            requiresRowValidation = false,
        });
        var registrationValidation = JObject.FromObject(new
        {
            type = EventType.Registration,
            requiresBrandsFile = false,
            requiresPartnershipsFile = false,
            organisationMemberCount = 10,
            registrationJourney = RegistrationJourney.CsoLargeProducer.ToString(),
            blobName,
        });

        await HttpClient.PostAsync(
            $"/v1/submissions/{submissionId}/events",
            new StringContent(antivirusCheck.ToString(), Encoding.UTF8, "application/json"));
        await HttpClient.PostAsync(
            $"/v1/submissions/{submissionId}/events",
            new StringContent(antivirusResult.ToString(), Encoding.UTF8, "application/json"));
        await HttpClient.PostAsync(
            $"/v1/submissions/{submissionId}/events",
            new StringContent(registrationValidation.ToString(), Encoding.UTF8, "application/json"));

        var submitPayload = new
        {
            submittedBy = "Integration",
            fileId,
            appReferenceNumber = "APP-DEC",
            isResubmission = false,
            registrationJourney = RegistrationJourney.CsoLargeProducer.ToString(),
        };
        var submitResponse = await HttpClient.PostAsJsonAsync($"/v1/submissions/{submissionId}/submit", submitPayload);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var regulatorEvent = TestRequests.SubmissionEvent.ValidRegulatorRegistrationDecisionEventCreateRequest();
        regulatorEvent["fileId"] = fileId;
        var regulatorResponse = await CreateEventAsync(submissionId, EventType.RegulatorRegistrationDecision, regulatorEvent);
        regulatorResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var decisionsResponse = await HttpClient.GetAsync(
            $"/v1/decisions?LastSyncTime=1990-01-01T00:00:00Z&SubmissionId={submissionId}&Type=Registration");

        var body = await AssertJsonArrayResponseAsync(decisionsResponse);
        body.Should().NotBeEmpty();
        var first = body[0] as JObject;
        first.Should().NotBeNull();
        Guid.Parse(first!["submissionId"]!.ToString()).Should().Be(submissionId);
        first["fileId"]!.Type.Should().Be(JTokenType.String);

        var registrationDecisionFeed = await HttpClient.GetAsync(
            "/v1/submissions/events/get-regulator-registration-decision?LastSyncTime=1990-01-01T00:00:00Z");
        var registrationDecisions = await AssertJsonArrayResponseAsync(registrationDecisionFeed);
        registrationDecisions.Should().Contain(t => Guid.Parse(t["submissionId"]!.ToString()) == submissionId);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetDecisions_ReturnsBadRequest_WhenLastSyncTimeIsUnparseable()
    {
        var submissionId = Guid.NewGuid();
        var response = await HttpClient.GetAsync(
            $"/v1/decisions?LastSyncTime=not-a-date&SubmissionId={submissionId}&Type=Producer");

        await AssertValidationProblemAsync(response, "LastSyncTime");
    }
}
