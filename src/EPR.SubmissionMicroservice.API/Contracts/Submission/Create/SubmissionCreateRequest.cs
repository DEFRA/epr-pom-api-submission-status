using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.API.Contracts.Submission.Create;

[ExcludeFromCodeCoverage]
public class SubmissionCreateRequest
{
    public Guid Id { get; set; }

    public SubmissionType SubmissionType { get; set; }

    public string SubmissionPeriod { get; set; }

    public DataSourceType DataSourceType { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public bool? IsResubmission { get; set; }

    public string? RegistrationJourney { get; set; }
}