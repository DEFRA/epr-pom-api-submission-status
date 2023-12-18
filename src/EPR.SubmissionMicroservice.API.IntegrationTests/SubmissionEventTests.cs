namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Data.Enums;
using FluentAssertions;
using TestSupport;

[TestClass]
public class SubmissionEventTests : TestBase
{
    private const string SubmissionEventsBasePath = "/v1/submissions/{0}/events";
    private const string SubmissionsBasePath = "/v1/submissions";

    [TestMethod]
    [DataRow(SubmissionType.Producer, EventType.AntivirusCheck)]
    [DataRow(SubmissionType.Producer, EventType.CheckSplitter)]
    [DataRow(SubmissionType.Producer, EventType.ProducerValidation)]
    [DataRow(SubmissionType.Registration, EventType.AntivirusCheck)]
    [DataRow(SubmissionType.Registration, EventType.Registration)]
    public async Task CreateEvent_ReturnsCreated_WhenSubmissionExists(SubmissionType submissionType, EventType eventType)
    {
        // Arrange
        var submissionRequest = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);
        await HttpClient.PostAsJsonAsync(SubmissionsBasePath, submissionRequest);

        var path = string.Format(SubmissionEventsBasePath, submissionRequest.Id);

        // Act
        var submissionEventRequest = new StringContent(
            TestRequests.SubmissionEvent.ValidSubmissionEventCreateRequest(eventType).ToString(),
            Encoding.UTF8,
            "application/json");
        var response = await HttpClient.PostAsync(path, submissionEventRequest);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.Created);
    }

    [TestMethod]
    [DataRow(EventType.AntivirusCheck)]
    [DataRow(EventType.CheckSplitter)]
    [DataRow(EventType.ProducerValidation)]
    [DataRow(EventType.Registration)]
    [DataRow(EventType.AntivirusResult)]
    public async Task CreateEvent_ReturnsBadRequest_WhenSubmissionNotExists(EventType eventType)
    {
        // Arrange
        var path = string.Format(SubmissionEventsBasePath, Guid.NewGuid());

        // Act
        var submissionEventRequest = new StringContent(
            TestRequests.SubmissionEvent.ValidSubmissionEventCreateRequest(eventType).ToString(),
            Encoding.UTF8,
            "application/json");
        var response = await HttpClient.PostAsync(path, submissionEventRequest);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }
}