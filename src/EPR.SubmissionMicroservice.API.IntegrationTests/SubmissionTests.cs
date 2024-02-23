namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Data.Enums;
using FluentAssertions;
using TestSupport;

[TestClass]
public class SubmissionTests : TestBase
{
    private const string BasePath = "/v1/submissions";

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task CreateSubmission_ReturnsCreated_WhenSubmissionNotExists(SubmissionType submissionType)
    {
        // Arrange
        var request = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);

        // Act
        var response = await HttpClient.PostAsJsonAsync(BasePath, request);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Created);
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task CreateSubmission_ReturnsBadRequest_WhenSubmissionExists(SubmissionType submissionType)
    {
        // Arrange
        var request = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);
        await HttpClient.PostAsJsonAsync(BasePath, request);

        // Act
        var response = await HttpClient.PostAsJsonAsync(BasePath, request);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task GetSubmission_ReturnsOK_WhenSubmissionExists(SubmissionType submissionType)
    {
        // Arrange
        var request = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);
        await HttpClient.PostAsJsonAsync(BasePath, request);
        var path = $"{BasePath}/{request.Id}";

        // Act
        var response = await HttpClient.GetAsync(path);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task GetSubmission_ReturnsNotFound_WhenSubmissionNotExists()
    {
        // Arrange
        var path = $"{BasePath}/{Guid.NewGuid()}";

        // Act
        var response = await HttpClient.GetAsync(path);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task GetSubmissions_ReturnsOK_WhenSubmissionsExists(SubmissionType submissionType)
    {
        // Arrange
        ReplaceOrganisationId();
        var request = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);
        await HttpClient.PostAsJsonAsync(BasePath, request);

        var pageSize = 1;
        var pageNumber = 1;

        // Act
        var response = await HttpClient.GetAsync($"{BasePath}?pagesize={pageSize}&pagenumber={pageNumber}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        RestoreOrganisationId();
    }

    [TestMethod]
    public async Task GetSubmissions_ReturnsOK_WhenBothSubmissionTypeExists()
    {
        // Arrange
        ReplaceOrganisationId();
        var request = TestRequests.Submission.ValidSubmissionCreateRequest(SubmissionType.Producer);
        await HttpClient.PostAsJsonAsync(BasePath, request);

        var pageSize = 1;
        var pageNumber = 1;

        // Act
        var response = await HttpClient.GetAsync($"{BasePath}?pagesize={pageSize}&pagenumber={pageNumber}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        RestoreOrganisationId();
    }

    [TestMethod]
    public async Task GetSubmissions_ReturnsZeroRecord_WhenSubmissionNotExists()
    {
        // Arrange
        ReplaceOrganisationId();

        var pageSize = 1;
        var pageNumber = 1;

        // Act
        var response = await HttpClient.GetAsync($"{BasePath}?pagesize={pageSize}&pagenumber={pageNumber}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        RestoreOrganisationId();
    }

    [TestMethod]
    public async Task GetSubmissionEvents_ReturnsOK_WhenRecordExistsAgainstBothSubmissionIdAndLastSyncTime()
    {
        // Arrrange
        var submissionId = Guid.Parse("4ccc06b9-f238-4f19-8f74-5d7d3fb79ff8");
        string lastSyncTime = "2024-01-01";

        // Act
        var response = await HttpClient.GetAsync($"{BasePath}/events/events-by-type/{submissionId}?LastSyncTime={lastSyncTime}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task GetSubmissionsByType_ReturnsOK_WhenRecordExistAgainstsOrganisationIdTypeAndYear()
    {
        // Arrrange
        var organisationId = Guid.Parse("92fb59d8-b6a7-4546-89a8-1ae938663993");
        string submissionType = "Producer";
        int year = 2023;

        // Act
        var response = await HttpClient.GetAsync($"{BasePath}/submissions?Type={submissionType}&OrganisationId={organisationId}&Year={year}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
    }

    private void ReplaceOrganisationId()
    {
        HttpClient.DefaultRequestHeaders.Remove("organisationId");
        HttpClient.DefaultRequestHeaders.Add("organisationId", Guid.NewGuid().ToString());
    }

    private void RestoreOrganisationId()
    {
        HttpClient.DefaultRequestHeaders.Remove("organisationId");
        HttpClient.DefaultRequestHeaders.Add("organisationId", OrganisationId);
    }
}