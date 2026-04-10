namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Data.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using TestSupport;

[TestClass]
public class ApplicationDetailsIntegrationTests : TestBase
{
    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetRegistrationApplicationDetails_ReturnsOk_WithSubmissionFields_WhenSubmissionMatchesQuery()
    {
        var submissionId = Guid.NewGuid();
        var createRequest = TestRequests.Submission.ValidSubmissionCreateRequest(SubmissionType.Registration);
        createRequest.Id = submissionId;
        createRequest.SubmissionPeriod = "January to December 2026";
        createRequest.RegistrationJourney = "CsoLargeProducer";

        (await HttpClient.PostAsJsonAsync("/v1/submissions", createRequest)).Should()
            .HaveStatusCode(HttpStatusCode.Created);

        var organisationId = Guid.Parse(OrganisationId);
        var path =
            $"/v1/submissions/get-registration-application-details?OrganisationId={organisationId}&SubmissionPeriod={createRequest.SubmissionPeriod}&LateFeeDeadline=2026-12-31&RegistrationJourney=CsoLargeProducer";

        var response = await HttpClient.GetAsync(path);
        var body = await AssertJsonObjectResponseAsync(response);

        Guid.Parse(body["submissionId"]!.ToString()).Should().Be(submissionId);
        body["registrationJourney"]!.Value<string>().Should().Be("CsoLargeProducer");
        body["applicationStatus"]!.ToString().Should().NotBeNullOrWhiteSpace();
        body["isSubmitted"]!.Type.Should().Be(JTokenType.Boolean);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetPackagingDataResubmissionApplicationDetails_ReturnsArray_WithSubmissionId_WhenProducerExistsForPeriod()
    {
        var submissionId = Guid.NewGuid();
        var createRequest = TestRequests.Submission.ValidSubmissionCreateRequest(SubmissionType.Producer);
        createRequest.Id = submissionId;
        createRequest.SubmissionPeriod = "January to December 2026";

        (await HttpClient.PostAsJsonAsync("/v1/submissions", createRequest)).Should()
            .HaveStatusCode(HttpStatusCode.Created);

        var organisationId = Guid.Parse(OrganisationId);
        var path =
            $"/v1/submissions/get-packaging-data-resubmission-application-details?OrganisationId={organisationId}&SubmissionPeriods={createRequest.SubmissionPeriod}";

        var response = await HttpClient.GetAsync(path);
        var body = await AssertJsonArrayResponseAsync(response);
        body.Count.Should().BeGreaterThan(0);
        var first = (JObject)body[0]!;
        Guid.Parse(first["submissionId"]!.ToString()).Should().Be(submissionId);
        first["isSubmitted"]!.Type.Should().Be(JTokenType.Boolean);
    }
}
