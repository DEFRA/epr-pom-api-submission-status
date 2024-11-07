namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

public class RegulatorOrganisationRegistrationDecisionGetResponse : AbstractSubmissionEventGetResponse
{
    public string Decision { get; set; }

    public string RegistrationReferenceNumber { get; set; }
}
