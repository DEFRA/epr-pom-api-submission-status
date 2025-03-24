namespace EPR.SubmissionMicroservice.Data.Entities.Submission;

using System.Diagnostics.CodeAnalysis;
using Common.Functions.Database.Entities.Interfaces;
using Enums;

[ExcludeFromCodeCoverage]
public class Submission : EntityWithId, ICreated
{
    public SubmissionType SubmissionType { get; set; }

    public string SubmissionPeriod { get; set; }

    public DataSourceType DataSourceType { get; set; }

    public Guid OrganisationId { get; set; }

    public Guid UserId { get; set; }

    public bool? IsSubmitted { get; set; }

    public bool? IsResubmission { get; set; }

    public Guid? ComplianceSchemeId { get; set; }

    public string? AppReferenceNumber { get; set; }

    public DateTime Created { get; set; }
}