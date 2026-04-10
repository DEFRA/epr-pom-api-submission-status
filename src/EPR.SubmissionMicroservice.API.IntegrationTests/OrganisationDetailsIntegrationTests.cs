namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Data.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using TestSupport;

[TestClass]
public class OrganisationDetailsIntegrationTests : TestBase
{
    /// <summary>
    /// Seeds the registration + antivirus + brand validation chain required by
    /// <see cref="EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionOrganisationDetailsGet.SubmissionOrganisationDetailsGetQueryHandler"/>,
    /// then asserts the JSON contract for a successful GET.
    /// </summary>
    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task GetSubmissionOrganisationDetails_ReturnsOk_WithExpectedFields_WhenBrandBlobChainExists()
    {
        var submissionId = Guid.NewGuid();
        var registrationSetId = Guid.NewGuid();
        var companyDetailsFileId = Guid.NewGuid();
        var brandsFileId = Guid.NewGuid();
        var registrationBlobName = $"reg-org-{Guid.NewGuid():N}.csv";
        var brandsBlobName = $"brands-org-{Guid.NewGuid():N}.csv";

        var createSubmission = TestRequests.Submission.ValidSubmissionCreateRequest(SubmissionType.Registration);
        createSubmission.Id = submissionId;
        createSubmission.SubmissionPeriod = "January to December 2026";
        createSubmission.RegistrationJourney = "CsoLargeProducer";
        (await HttpClient.PostAsJsonAsync("/v1/submissions", createSubmission)).Should()
            .HaveStatusCode(HttpStatusCode.Created);

        var events = new[]
        {
            JObject.FromObject(new
            {
                type = EventType.AntivirusCheck,
                fileId = companyDetailsFileId,
                fileType = FileType.CompanyDetails,
                fileName = "CompanyDetails.csv",
                registrationSetId,
            }),
            JObject.FromObject(new
            {
                type = EventType.AntivirusResult,
                fileId = companyDetailsFileId,
                antivirusScanResult = AntivirusScanResult.Success,
                antivirusScanTrigger = AntivirusScanTrigger.Upload,
                blobName = registrationBlobName,
                requiresRowValidation = false,
            }),
            JObject.FromObject(new
            {
                type = EventType.Registration,
                requiresBrandsFile = true,
                requiresPartnershipsFile = true,
                organisationMemberCount = 10,
                registrationJourney = "CsoLargeProducer",
                blobName = registrationBlobName,
            }),
            JObject.FromObject(new
            {
                type = EventType.AntivirusCheck,
                fileId = brandsFileId,
                fileType = FileType.Brands,
                fileName = "Brands.csv",
                registrationSetId,
            }),
            JObject.FromObject(new
            {
                type = EventType.AntivirusResult,
                fileId = brandsFileId,
                antivirusScanResult = AntivirusScanResult.Success,
                antivirusScanTrigger = AntivirusScanTrigger.Upload,
                blobName = brandsBlobName,
                requiresRowValidation = false,
            }),
            JObject.FromObject(new
            {
                type = EventType.BrandValidation,
                blobName = brandsBlobName,
                blobContainerName = "registration-upload-container",
                errors = new JArray(),
            }),
        };

        foreach (var ev in events)
        {
            var post = await HttpClient.PostAsync(
                $"/v1/submissions/{submissionId}/events",
                new StringContent(ev.ToString(), Encoding.UTF8, "application/json"));
            post.Should().HaveStatusCode(HttpStatusCode.Created, $"failed posting event type {ev["type"]}");
        }

        var response = await HttpClient.GetAsync(
            $"/v1/submissions/{submissionId}/organisation-details?blobName={Uri.EscapeDataString(brandsBlobName)}");

        var body = await AssertJsonObjectResponseAsync(response);
        body["blobName"]!.Value<string>().Should().Be(registrationBlobName);
        body["submissionPeriod"]!.Value<string>().Should().Be("January to December 2026");
        body["registrationJourney"]!.Value<string>().Should().Be("CsoLargeProducer");
    }
}
