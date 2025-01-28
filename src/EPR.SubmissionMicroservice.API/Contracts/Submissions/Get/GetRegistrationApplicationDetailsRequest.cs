using System.Diagnostics.CodeAnalysis;

namespace EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;

[ExcludeFromCodeCoverage]
public class GetRegistrationApplicationDetailsRequest
{
    public Guid OrganisationId { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public string SubmissionPeriod { get; set; } = null!;
}