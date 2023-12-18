namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Data.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using TestSupport;

[TestClass]
public class ValidationEventErrorTests : TestBase
{
    private const string ValidationEventErrorBasePath = "/v1/submissions/{0}/producer-validations";
    private const string ValidationEventWarningBasePath = "/v1/submissions/{0}/producer-warning-validations";
    private const string SubmissionsBasePath = "/v1/submissions";
    private const string SubmissionEventsBasePath = "/v1/submissions/{0}/events";

    [TestMethod]
    public async Task Get_ReturnsOK_WhenProducerValidationExists()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        var submissionRequest = TestRequests.Submission.ValidSubmissionCreateRequest(SubmissionType.Producer);
        submissionRequest.Id = submissionId;
        await HttpClient.PostAsJsonAsync(SubmissionsBasePath, submissionRequest);

        var request =
            TestRequests.SubmissionEvent.ValidSubmissionEventCreateRequest(EventType.ProducerValidation);
        request["validationErrors"] = new JArray
        {
            TestRequests.ValidationEventError.ValidValidationEventError(ValidationType.ProducerValidation)
        };
        var submissionEventsPath = string.Format(SubmissionEventsBasePath, submissionId);

        var requestStringContent = new StringContent(
            request.ToString(),
            Encoding.UTF8,
            "application/json");
        var producerValidationCreateResponse = await HttpClient.PostAsync(submissionEventsPath, requestStringContent);

        // Act
        var errorPath = string.Format(ValidationEventErrorBasePath, submissionId);
        var response = await HttpClient.GetAsync(errorPath);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Get_ReturnsOK_WhenProducerValidationWarningsExists()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        var submissionRequest = TestRequests.Submission.ValidSubmissionCreateRequest(SubmissionType.Producer);
        submissionRequest.Id = submissionId;
        await HttpClient.PostAsJsonAsync(SubmissionsBasePath, submissionRequest);

        var request = TestRequests.SubmissionEvent.ValidSubmissionEventCreateRequest(EventType.ProducerValidation);
        request["validationWarnings"] = new JArray
        {
            TestRequests.ValidationEventError.ValidValidationEventError(ValidationType.ProducerValidation)
        };
        var submissionEventsPath = string.Format(SubmissionEventsBasePath, submissionId);

        var requestStringContent = new StringContent(
            request.ToString(),
            Encoding.UTF8,
            "application/json");
        var producerValidationCreateResponse = await HttpClient.PostAsync(submissionEventsPath, requestStringContent);

        // Act
        var errorPath = string.Format(ValidationEventWarningBasePath, submissionId);
        var response = await HttpClient.GetAsync(errorPath);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
    }
}