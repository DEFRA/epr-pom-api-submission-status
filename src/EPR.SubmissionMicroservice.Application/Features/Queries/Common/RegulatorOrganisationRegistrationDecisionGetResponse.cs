namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

public class RegulatorOrganisationRegistrationDecisionGetResponse
{
    public string AppReferenceNumber { get; set; }

    public DateTime Created { get; set; }

    public string Comments { get; set; } = string.Empty;

    public string Decision { get; set; }

    public DateTime? DecisionDate { get; set; }

    public string RegistrationReferenceNumber { get; set; }

    public Guid SubmissionId { get; set; }

    public string Type { get; set; }
}