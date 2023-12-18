namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;
using Data.Enums;

[ExcludeFromCodeCoverage]
public abstract class AbstractSubmissionGetResponse
{
    public Guid Id { get; set; }

    public DataSourceType DataSourceType { get; set; }

    public SubmissionType SubmissionType { get; set; }

    public string SubmissionPeriod { get; set; }

    public Guid OrganisationId { get; set; }

    public Guid UserId { get; set; }

    public DateTime Created { get; set; }

    public bool ValidationPass { get; set; }

    public List<string> Errors { get; set; } = new();

    public bool IsSubmitted { get; set; }

    public abstract bool HasValidFile { get; }

    public Guid? ComplianceSchemeId { get; set; }
}