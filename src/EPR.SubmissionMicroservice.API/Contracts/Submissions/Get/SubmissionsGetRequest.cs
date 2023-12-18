namespace EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;

using Data.Enums;

public class SubmissionsGetRequest
{
    public string? Periods { get; set; }

    public SubmissionType? Type { get; set; }

    public int? Limit { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public bool? IsFirstComplianceScheme { get; set; }
}