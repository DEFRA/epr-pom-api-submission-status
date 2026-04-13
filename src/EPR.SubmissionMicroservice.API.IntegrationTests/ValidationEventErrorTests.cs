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
    private const string OrganisationValidationErrorPath = "/v1/submissions/{0}/organisation-details-errors";
    private const string OrganisationValidationWarningPath = "/v1/submissions/{0}/organisation-details-warnings";
    private const string SubmissionsBasePath = "/v1/submissions";
    private const string SubmissionEventsBasePath = "/v1/submissions/{0}/events";

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetProducerValidationErrors_ReturnsErrors_WhenProducerValidationExists()
    {
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
        producerValidationCreateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var errorPath = string.Format(ValidationEventErrorBasePath, submissionId);
        var response = await HttpClient.GetAsync(errorPath);

        await AssertJsonArrayResponseAsync(response);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetProducerValidationWarnings_ReturnsWarnings_WhenProducerValidationWarningsExist()
    {
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
        producerValidationCreateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var errorPath = string.Format(ValidationEventWarningBasePath, submissionId);
        var response = await HttpClient.GetAsync(errorPath);

        await AssertJsonArrayResponseAsync(response);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetProducerValidationWarnings_ReturnsWarnings_WhenCheckSplitterWarningsExist()
    {
        var submissionId = Guid.NewGuid();
        var submissionRequest = TestRequests.Submission.ValidSubmissionCreateRequest(SubmissionType.Producer);
        submissionRequest.Id = submissionId;
        await HttpClient.PostAsJsonAsync(SubmissionsBasePath, submissionRequest);

        var request = TestRequests.SubmissionEvent.ValidSubmissionEventCreateRequest(EventType.CheckSplitter);
        request["validationWarnings"] = new JArray
        {
            TestRequests.ValidationEventError.ValidValidationEventWarning(ValidationType.CheckSplitter)
        };
        var submissionEventsPath = string.Format(SubmissionEventsBasePath, submissionId);

        var requestStringContent = new StringContent(
            request.ToString(),
            Encoding.UTF8,
            "application/json");
        var checkSplitterCreateResponse = await HttpClient.PostAsync(submissionEventsPath, requestStringContent);
        checkSplitterCreateResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var errorPath = string.Format(ValidationEventWarningBasePath, submissionId);
        var response = await HttpClient.GetAsync(errorPath);

        await AssertJsonArrayResponseAsync(response);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetOrganisationDetailValidationErrors_ReturnsBadRequest_WhenSubmissionDoesNotExist()
    {
        var response = await HttpClient.GetAsync(string.Format(OrganisationValidationErrorPath, Guid.NewGuid()));
        await AssertValidationProblemAsync(response, "SubmissionId");
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetOrganisationDetailValidationWarnings_ReturnsBadRequest_WhenSubmissionDoesNotExist()
    {
        var response = await HttpClient.GetAsync(string.Format(OrganisationValidationWarningPath, Guid.NewGuid()));
        await AssertValidationProblemAsync(response, "SubmissionId");
    }
}