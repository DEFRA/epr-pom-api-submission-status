namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using Data.Enums;
using FluentAssertions;

[TestClass]
public class HeaderAndBoundaryTests : TestBase
{
    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissions_ReturnsBadRequest_WhenOrganisationIdHeaderIsMissing()
    {
        RemoveHeader("organisationId");
        var response = await HttpClient.GetAsync("/v1/submissions?Limit=1");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        SetHeader("organisationId", OrganisationId);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissions_ReturnsOk_WhenUserIdHeaderIsMissing()
    {
        await CreateSubmissionsAsync(SubmissionType.Producer, 1);
        RemoveHeader("userId");
        var response = await HttpClient.GetAsync("/v1/submissions?Limit=1");
        var body = await AssertJsonArrayResponseAsync(response);
        body.Count.Should().BeGreaterThan(0);
        SetHeader("userId", Guid.NewGuid().ToString());
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissions_ReturnsBadRequest_WhenTypeQueryIsInvalid()
    {
        var response = await HttpClient.GetAsync("/v1/submissions?Type=InvalidType");
        await AssertValidationProblemAsync(response, "Type");
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissions_ReturnsOk_WhenLimitIsLarge()
    {
        // SubmissionsGetQueryHandler returns at most one row per SubmissionPeriod (latest by Created).
        var idFirstPeriod = Guid.NewGuid();
        var idSecondPeriod = Guid.NewGuid();
        (await CreateSubmissionAsync(SubmissionType.Producer, idFirstPeriod, "2022")).Should()
            .HaveStatusCode(HttpStatusCode.Created);
        (await CreateSubmissionAsync(SubmissionType.Producer, idSecondPeriod, "2023")).Should()
            .HaveStatusCode(HttpStatusCode.Created);

        var response = await HttpClient.GetAsync("/v1/submissions?Limit=10000");
        var body = await AssertJsonArrayResponseAsync(response);
        var returnedIds = body.Select(t => Guid.Parse(t["id"]!.ToString())).ToList();
        returnedIds.Should().Contain(idFirstPeriod);
        returnedIds.Should().Contain(idSecondPeriod);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmission_ReturnsUnauthorized_WhenOrganisationIdDoesNotOwnSubmission()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Producer, submissionId);

        var otherOrganisationId = Guid.NewGuid().ToString();
        SetHeader("organisationId", otherOrganisationId);

        var response = await HttpClient.GetAsync($"/v1/submissions/{submissionId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        SetHeader("organisationId", OrganisationId);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissions_ReturnsOk_WhenLimitIsZero()
    {
        await CreateSubmissionsAsync(SubmissionType.Producer, 1);
        var response = await HttpClient.GetAsync("/v1/submissions?Limit=0");
        var body = await AssertJsonArrayResponseAsync(response);
        body.Count.Should().BeLessOrEqualTo(1);
    }
}
