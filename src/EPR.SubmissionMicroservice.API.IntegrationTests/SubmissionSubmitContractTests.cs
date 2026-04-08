namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Data.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;

[TestClass]
public class SubmissionSubmitContractTests : TestBase
{
    /// <summary>
    /// Seeds the chain required by <see cref="EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.PomSubmissionEventHelper.VerifyFileIdIsForValidFileAsync"/>:
    /// AntivirusResult (with blob), CheckSplitter (same blob + DataCount), and that many valid ProducerValidation events for the blob.
    /// </summary>
    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task Submit_ReturnsNoContent_WhenProducerPipelineIsValid()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Producer, submissionId);

        var fileId = Guid.NewGuid();
        // Must be unique per run: VerifyFileIdIsForValidFileAsync counts ProducerValidation events globally by BlobName.
        var blobName = $"integration-producer-submit-{Guid.NewGuid():N}.csv";
        const string blobContainerName = "pom-upload-container";

        var antivirusCheck = JObject.FromObject(new
        {
            type = EventType.AntivirusCheck,
            fileId,
            fileType = FileType.Pom,
            fileName = "pom.csv",
        });
        var antivirusResult = JObject.FromObject(new
        {
            type = EventType.AntivirusResult,
            fileId,
            antivirusScanResult = AntivirusScanResult.Success,
            antivirusScanTrigger = AntivirusScanTrigger.Upload,
            blobName,
            requiresRowValidation = false,
        });
        var checkSplitter = JObject.FromObject(new
        {
            type = EventType.CheckSplitter,
            dataCount = 1,
            blobName,
            blobContainerName,
        });
        var producerValidation = JObject.FromObject(new
        {
            type = EventType.ProducerValidation,
            producerId = "123456",
            blobName,
            blobContainerName,
        });

        foreach (var ev in new[] { antivirusCheck, antivirusResult, checkSplitter, producerValidation })
        {
            (await HttpClient.PostAsync(
                $"/v1/submissions/{submissionId}/events",
                new StringContent(ev.ToString(), Encoding.UTF8, "application/json"))).Should()
                .HaveStatusCode(HttpStatusCode.Created);
        }

        var submitPayload = new
        {
            submittedBy = "Integration",
            fileId,
            appReferenceNumber = "APP-PROD-SUBMIT",
            isResubmission = false,
        };

        var response = await HttpClient.PostAsJsonAsync($"/v1/submissions/{submissionId}/submit", submitPayload);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await HttpClient.GetAsync($"/v1/submissions/{submissionId}");
        var submission = await AssertJsonObjectResponseAsync(getResponse);
        Guid.Parse(submission["id"]!.ToString()).Should().Be(submissionId);
        submission["isSubmitted"]!.Value<bool>().Should().BeTrue();
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task Submit_ReturnsNoContent_WhenRegistrationPipelineIsValid()
    {
        var submissionId = Guid.NewGuid();
        await CreateSubmissionAsync(SubmissionType.Registration, submissionId);

        var fileId = Guid.NewGuid();
        const string blobName = "integration-submit-blob.csv";

        var antivirusCheck = JObject.FromObject(new
        {
            type = EventType.AntivirusCheck,
            fileId,
            fileType = FileType.CompanyDetails,
            fileName = "company.csv",
        });
        var antivirusResult = JObject.FromObject(new
        {
            type = EventType.AntivirusResult,
            fileId,
            antivirusScanResult = AntivirusScanResult.Success,
            antivirusScanTrigger = AntivirusScanTrigger.Upload,
            blobName,
            requiresRowValidation = false,
        });
        var registrationValidation = JObject.FromObject(new
        {
            type = EventType.Registration,
            requiresBrandsFile = false,
            requiresPartnershipsFile = false,
            organisationMemberCount = 10,
            registrationJourney = "CsoLargeProducer",
            blobName,
        });

        (await HttpClient.PostAsync(
            $"/v1/submissions/{submissionId}/events",
            new StringContent(antivirusCheck.ToString(), Encoding.UTF8, "application/json"))).Should()
            .HaveStatusCode(HttpStatusCode.Created);
        (await HttpClient.PostAsync(
            $"/v1/submissions/{submissionId}/events",
            new StringContent(antivirusResult.ToString(), Encoding.UTF8, "application/json"))).Should()
            .HaveStatusCode(HttpStatusCode.Created);
        (await HttpClient.PostAsync(
            $"/v1/submissions/{submissionId}/events",
            new StringContent(registrationValidation.ToString(), Encoding.UTF8, "application/json"))).Should()
            .HaveStatusCode(HttpStatusCode.Created);

        var submitPayload = new
        {
            submittedBy = "Integration",
            fileId,
            appReferenceNumber = "APP-SUBMIT",
            isResubmission = false,
            registrationJourney = "CsoLargeProducer",
        };

        var response = await HttpClient.PostAsJsonAsync($"/v1/submissions/{submissionId}/submit", submitPayload);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await HttpClient.GetAsync($"/v1/submissions/{submissionId}");
        var submission = await AssertJsonObjectResponseAsync(getResponse);
        submission["isSubmitted"]!.Value<bool>().Should().BeTrue();
    }
}
