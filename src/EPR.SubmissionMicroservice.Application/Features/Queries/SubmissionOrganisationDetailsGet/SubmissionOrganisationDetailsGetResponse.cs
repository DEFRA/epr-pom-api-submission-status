namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionOrganisationDetailsGet;

public class SubmissionOrganisationDetailsGetResponse
{
    public string BlobName { get; set; }

    public string SubmissionPeriod { get; set; }

    public string? RegistrationJourney { get; set; }
}