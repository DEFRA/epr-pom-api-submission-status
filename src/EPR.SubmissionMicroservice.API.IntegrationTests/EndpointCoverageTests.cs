namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Data.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using TestSupport;

[TestClass]
public class EndpointCoverageTests : TestBase
{
    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissionFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        var response = await HttpClient.GetAsync($"/v1/submissions/files/{Guid.NewGuid()}");
        await AssertProblemAsync(response, StatusCodes.Status404NotFound);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissionFile_ReturnsOk_WhenAntivirusCheckEventExists()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Producer, submissionId);
        var eventRequest = TestRequests.SubmissionEvent.ValidAntivirusCheckEventCreateRequest();
        var fileId = eventRequest["fileId"]!.Value<Guid>();
        await CreateEventAsync(submissionId, EventType.AntivirusCheck, eventRequest);

        var response = await HttpClient.GetAsync($"/v1/submissions/files/{fileId}");
        var body = await AssertJsonObjectResponseAsync(response);
        Guid.Parse(body["fileId"]!.ToString()).Should().Be(fileId);
        AssertJsonObjectHasKeys(body, "submissionId", "fileName", "fileType", "submissionPeriod", "organisationId");
        body["fileName"]!.ToString().Should().Be("MyFile.csv");
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissionUploadedFile_ReturnsNotFound_WhenFileDoesNotExist()
    {
        var response = await HttpClient.GetAsync($"/v1/submissions/{Guid.NewGuid()}/uploadedfile/{Guid.NewGuid()}");
        await AssertProblemAsync(response, StatusCodes.Status404NotFound);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissionUploadedFile_ReturnsOk_WhenAntivirusResultEventExists()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Producer, submissionId);
        var eventRequest = TestRequests.SubmissionEvent.ValidAntivirusResultEventCreateRequest();
        var fileId = eventRequest["fileId"]!.Value<Guid>();
        await CreateEventAsync(submissionId, EventType.AntivirusResult, eventRequest);

        var response = await HttpClient.GetAsync($"/v1/submissions/{submissionId}/uploadedfile/{fileId}");
        var body = await AssertJsonObjectResponseAsync(response);
        Guid.Parse(body["fileId"]!.ToString()).Should().Be(fileId);
        AssertJsonObjectHasKeys(body, "submissionId", "blobName", "antivirusScanResult", "organisationId");
        Guid.Parse(body["submissionId"]!.ToString()).Should().Be(submissionId);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissionOrganisationDetails_ReturnsNotFound_WhenBlobNameDoesNotMatch()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Registration, submissionId);
        var response = await HttpClient.GetAsync($"/v1/submissions/{submissionId}/organisation-details?blobName=missing.csv");
        await AssertProblemAsync(response, StatusCodes.Status404NotFound);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task Submit_ReturnsBadRequest_WhenSubmissionDoesNotExist()
    {
        var payload = JObject.FromObject(new
        {
            submittedBy = "Integration Test",
            fileId = Guid.NewGuid(),
            appReferenceNumber = "APP-123",
            isResubmission = false,
            registrationJourney = RegistrationJourney.CsoLargeProducer.ToString(),
        });

        var response = await HttpClient.PostAsJsonAsync($"/v1/submissions/{Guid.NewGuid()}/submit", payload);
        await AssertValidationProblemAsync(response, "request");
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetRegistrationApplicationDetails_ReturnsNoContent_WhenNoMatchingSubmissionExists()
    {
        var organisationId = Guid.NewGuid();
        var path =
            $"/v1/submissions/get-registration-application-details?OrganisationId={organisationId}&SubmissionPeriod=January to December 2026&LateFeeDeadline=2026-01-01";
        var response = await HttpClient.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Smoke: valid JSON array + media type. Deeper seeded assertions live in <see cref="ApplicationDetailsIntegrationTests"/>.
    /// </summary>
    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetPackagingDataResubmissionApplicationDetails_ReturnsOk_WhenRequestIsValid()
    {
        var organisationId = Guid.Parse(OrganisationId);
        var path =
            $"/v1/submissions/get-packaging-data-resubmission-application-details?OrganisationId={organisationId}&SubmissionPeriods=January to December 2026";
        var response = await HttpClient.GetAsync(path);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        await AssertJsonArrayResponseAsync(response);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetRegulatorPomDecision_ReturnsOk_WhenRequestIsValid()
    {
        var response =
            await HttpClient.GetAsync("/v1/submissions/events/get-regulator-pom-decision?LastSyncTime=2024-01-01");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var body = await AssertJsonArrayResponseAsync(response);
        if (body.Count > 0)
        {
            AssertJsonArrayElementsHaveKeys(body, "submissionId", "type", "created");
        }
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetRegulatorPomDecision_ReturnsBadRequest_WhenLastSyncTimeIsMissing()
    {
        var response = await HttpClient.GetAsync("/v1/submissions/events/get-regulator-pom-decision?LastSyncTime=");
        await AssertValidationProblemAsync(response, "LastSyncTime");
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetOrganisationRegistrationEvents_ReturnsOk_WhenRequestIsValid()
    {
        var response =
            await HttpClient.GetAsync("/v1/submissions/events/organisation-registration?LastSyncTime=2024-01-01");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var body = await AssertJsonArrayResponseAsync(response);
        if (body.Count > 0)
        {
            AssertJsonArrayElementsHaveKeys(body, "submissionId", "type", "created");
        }
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetOrganisationRegistrationEvents_ReturnsBadRequest_WhenLastSyncTimeIsMissing()
    {
        var response =
            await HttpClient.GetAsync("/v1/submissions/events/organisation-registration?LastSyncTime=");
        await AssertValidationProblemAsync(response, "LastSyncTime");
    }
}
