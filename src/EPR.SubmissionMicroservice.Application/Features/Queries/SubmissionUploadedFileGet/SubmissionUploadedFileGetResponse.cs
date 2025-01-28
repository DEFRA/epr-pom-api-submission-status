namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionUploadedFileGet;

using System.Diagnostics.CodeAnalysis;
using Data.Enums;

[ExcludeFromCodeCoverage]
public class SubmissionUploadedFileGetResponse
{
    public Guid SubmissionId { get; set; }

    public SubmissionType SubmissionType { get; set; }

    public Guid FileId { get; set; }

    public Guid UserId { get; set; }

    public Guid OrganisationId { get; set; }

    public AntivirusScanResult AntivirusScanResult { get; set; }

    public AntivirusScanTrigger? AntivirusScanTrigger { get; set; }

    public string BlobName { get; set; }

    public List<string> Errors { get; set; }
}