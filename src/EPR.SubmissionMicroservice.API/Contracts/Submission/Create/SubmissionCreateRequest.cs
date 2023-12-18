namespace EPR.SubmissionMicroservice.API.Contracts.Submission.Create;

using Data.Enums;

public class SubmissionCreateRequest
{
    public Guid Id { get; set; }

    public SubmissionType SubmissionType { get; set; }

    public string SubmissionPeriod { get; set; }

    public DataSourceType DataSourceType { get; set; }

    public Guid? ComplianceSchemeId { get; set; }
}