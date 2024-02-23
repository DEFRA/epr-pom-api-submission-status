namespace EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;

using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

[ExcludeFromCodeCoverage]
public class SubmissionGetRequest
{
    public Guid OrganisationId { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public int? Year { get; set; }

    public SubmissionType Type { get; set; }
}