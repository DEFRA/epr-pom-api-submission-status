namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using Data.Enums;
using FluentAssertions;
using TestSupport;

[TestClass]
public class EventTypeContractTests : TestBase
{
    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task CreateEvent_ReturnsCreated_WhenFileDownloadCheckOnProducerSubmission()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Producer, submissionId);

        var response = await CreateEventAsync(submissionId, EventType.FileDownloadCheck, TestRequests.SubmissionEvent.ValidFileDownloadCheckEventCreateRequest());

        response.Should().HaveStatusCode(HttpStatusCode.Created);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task CreateEvent_ReturnsCreated_WhenRegistrationApplicationSubmittedOnRegistrationSubmission()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Registration, submissionId);

        var response = await CreateEventAsync(
            submissionId,
            EventType.RegistrationApplicationSubmitted,
            TestRequests.SubmissionEvent.ValidRegistrationApplicationSubmittedEventCreateRequest());

        response.Should().HaveStatusCode(HttpStatusCode.Created);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task CreateEvent_ReturnsCreated_WhenSubsidiariesBulkUploadCompleteOnSubsidiarySubmission()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Subsidiary, submissionId);

        var response = await CreateEventAsync(
            submissionId,
            EventType.SubsidiariesBulkUploadComplete,
            TestRequests.SubmissionEvent.ValidSubsidiariesBulkUploadCompleteEventCreateRequest());

        response.Should().HaveStatusCode(HttpStatusCode.Created);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task CreateEvent_Persists_WhenPackagingResubmissionReferenceNumberCreatedOnProducerSubmission()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Producer, submissionId);

        var createResponse = await CreateEventAsync(
            submissionId,
            EventType.PackagingResubmissionReferenceNumberCreated,
            TestRequests.SubmissionEvent.ValidPackagingResubmissionReferenceNumberCreatedRequest());

        createResponse.Should().HaveStatusCode(HttpStatusCode.Created);

        var queryResponse = await HttpClient.GetAsync(
            $"/v1/submissions/events/events-by-type/{submissionId}?LastSyncTime=2000-01-01T00:00:00Z");
        queryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
