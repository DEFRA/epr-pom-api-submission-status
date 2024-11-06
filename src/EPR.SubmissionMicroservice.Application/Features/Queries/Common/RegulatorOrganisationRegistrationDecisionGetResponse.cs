namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

public class RegulatorOrganisationRegistrationDecisionGetResponse : AbstractSubmissionEventGetResponse
{
    public string Comments { get; set; }

    public string Decision { get; set; }

    public DateTime Created { get; set; }
}
