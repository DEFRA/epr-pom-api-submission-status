namespace EPR.SubmissionMicroservice.API.IntegrationTests;

using System.Net;
using Data.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;

[TestClass]
public class GoldenPathScenariosTests : TestBase
{
    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task ProducerSubmission_GoldenPath_CreateThenEventThenQuery()
    {
        var submissionId = Guid.NewGuid();

        var createSubmissionResponse = await CreateSubmissionAsync(SubmissionType.Producer, submissionId);
        createSubmissionResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createEventResponse = await CreateEventAsync(submissionId, EventType.ProducerValidation);
        createEventResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var getSubmissionResponse = await HttpClient.GetAsync($"/v1/submissions/{submissionId}");
        var submissionBody = await AssertJsonObjectResponseAsync(getSubmissionResponse);
        Guid.Parse(submissionBody["id"]!.ToString()).Should().Be(submissionId);
        submissionBody["organisationId"]!.ToString().Should().Be(OrganisationId);

        var getEventsResponse = await HttpClient.GetAsync($"/v1/submissions/events/events-by-type/{submissionId}?LastSyncTime=2000-01-01T00:00:00Z");
        getEventsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var eventsBody = await ReadJsonAsync(getEventsResponse);
        AssertJsonObjectHasKeys(eventsBody, "submittedEvents", "regulatorDecisionEvents", "antivirusCheckEvents");
        eventsBody["submittedEvents"]!.Type.Should().Be(JTokenType.Array);
        eventsBody["regulatorDecisionEvents"]!.Type.Should().Be(JTokenType.Array);
        eventsBody["antivirusCheckEvents"]!.Type.Should().Be(JTokenType.Array);
    }

    [TestMethod]
    [TestProperty("Category", "IntegrationTest")]
    public async Task Decisions_GoldenPath_ValidQueryReturnsOkWithArray()
    {
        var submissionId = Guid.NewGuid();
        var response = await HttpClient.GetAsync(
            $"/v1/decisions?LastSyncTime=2000-01-01T00:00:00Z&SubmissionId={submissionId}&Type=Producer");

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        await AssertJsonArrayResponseAsync(response);
    }
}
