namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Data.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using TestSupport;

[TestClass]
public class SubmissionTests : TestBase
{
    private const string BasePath = "/v1/submissions";

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task CreateSubmission_ReturnsCreatedAndLocation_WhenSubmissionDoesNotExist(SubmissionType submissionType)
    {
        var request = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);

        var response = await HttpClient.PostAsJsonAsync(BasePath, request);

        response.Should().HaveStatusCode(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().Contain(request.Id.ToString());
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task CreateSubmission_ReturnsValidationProblem_WhenSubmissionAlreadyExists(SubmissionType submissionType)
    {
        var request = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);
        await HttpClient.PostAsJsonAsync(BasePath, request);

        var response = await HttpClient.PostAsJsonAsync(BasePath, request);

        await AssertProblemAsync(response, StatusCodes.Status400BadRequest);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task GetSubmission_ReturnsSubmissionBody_WhenSubmissionExists(SubmissionType submissionType)
    {
        var request = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);
        await HttpClient.PostAsJsonAsync(BasePath, request);
        var path = $"{BasePath}/{request.Id}";

        var response = await HttpClient.GetAsync(path);

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        Guid.Parse(body["id"]!.ToString()).Should().Be(request.Id);
        body["submissionType"].Should().NotBeNull();
        body["organisationId"]!.ToString().Should().Be(OrganisationId);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmission_ReturnsProblemDetails_WhenSubmissionDoesNotExist()
    {
        var path = $"{BasePath}/{Guid.NewGuid()}";
        var response = await HttpClient.GetAsync(path);
        await AssertProblemAsync(response, StatusCodes.Status404NotFound);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task GetSubmissions_ReturnsCollection_WhenSubmissionExists(SubmissionType submissionType)
    {
        var request = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);
        await HttpClient.PostAsJsonAsync(BasePath, request);

        var response = await HttpClient.GetAsync($"{BasePath}?Limit=10&Type={submissionType}");
        var body = await AssertJsonArrayResponseAsync(response);
        body.Count.Should().BeGreaterThan(0);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissions_ReturnsAtLeastOneSubmission_WhenProducerAndRegistrationExist()
    {
        await CreateSubmissionAsync(SubmissionType.Producer);
        await CreateSubmissionAsync(SubmissionType.Registration);

        var response = await HttpClient.GetAsync($"{BasePath}?Limit=50");
        var body = await AssertJsonArrayResponseAsync(response);
        body.Count.Should().BeGreaterThan(0);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissions_ReturnsOkWithCollection_WhenNoSubmissionExists()
    {
        SetHeader("organisationId", Guid.NewGuid().ToString());
        var response = await HttpClient.GetAsync($"{BasePath}?Limit=10");
        var body = await AssertJsonArrayResponseAsync(response);
        body.Should().BeEmpty();
        SetHeader("organisationId", OrganisationId);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissions_RespectsLimit_ForDeterministicFixtureSet()
    {
        await CreateSubmissionsAsync(SubmissionType.Producer, 3);

        var response = await HttpClient.GetAsync($"{BasePath}?Limit=2&Type={SubmissionType.Producer}");
        var body = await AssertJsonArrayResponseAsync(response);
        body.Count.Should().BeLessOrEqualTo(2);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissionEvents_ReturnsOk_WhenLastSyncTimeIsValid()
    {
        var submissionId = Guid.NewGuid();
        var response = await HttpClient.GetAsync($"{BasePath}/events/events-by-type/{submissionId}?LastSyncTime=2024-01-01T00:00:00Z");
        response.Should().HaveStatusCode(HttpStatusCode.OK);
    }

}