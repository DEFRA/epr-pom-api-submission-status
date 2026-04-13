namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Data.Enums;
using Newtonsoft.Json.Linq;
using TestSupport;

[TestClass]
public class QueryMappingContractTests : TestBase
{
    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissions_FiltersByCommaSeparatedPeriods_WhenPeriodsQueryProvided()
    {
        var req2025 = TestRequests.Submission.ValidSubmissionCreateRequest(SubmissionType.Producer);
        req2025.SubmissionPeriod = "January to December 2025";
        var req2026 = TestRequests.Submission.ValidSubmissionCreateRequest(SubmissionType.Producer);
        req2026.Id = Guid.NewGuid();
        req2026.SubmissionPeriod = "January to December 2026";

        (await HttpClient.PostAsJsonAsync("/v1/submissions", req2025)).Should().HaveStatusCode(HttpStatusCode.Created);
        (await HttpClient.PostAsJsonAsync("/v1/submissions", req2026)).Should().HaveStatusCode(HttpStatusCode.Created);

        var response = await HttpClient.GetAsync($"/v1/submissions?Periods={req2025.SubmissionPeriod},{req2026.SubmissionPeriod}&Type=Producer&Limit=10");
        var body = await AssertJsonArrayResponseAsync(response);

        var periods = body.Select(t => t["submissionPeriod"]!.ToString()).Distinct().ToList();
        periods.Should().Contain("January to December 2025");
        periods.Should().Contain("January to December 2026");
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissionsByType_ReturnsSubmissionRows_WithExpectedKeys_WhenSubmissionExistsForYear()
    {
        var organisationId = Guid.Parse(OrganisationId);
        var year = DateTime.UtcNow.Year;

        var request = TestRequests.Submission.ValidSubmissionCreateRequest(SubmissionType.Registration);
        await HttpClient.PostAsJsonAsync("/v1/submissions", request);

        var response = await HttpClient.GetAsync(
            $"/v1/submissions/submissions?Type=Registration&OrganisationId={organisationId}&Year={year}");

        var body = await AssertJsonArrayResponseAsync(response);
        body.Should().NotBeEmpty();

        var row = body[0] as JObject;
        row.Should().NotBeNull();
        Guid.Parse(row!["submissionId"]!.ToString()).Should().Be(request.Id);
        row["submissionPeriod"]!.ToString().Should().Be(request.SubmissionPeriod);
        row["year"]!.Value<int>().Should().Be(year);
    }
}
