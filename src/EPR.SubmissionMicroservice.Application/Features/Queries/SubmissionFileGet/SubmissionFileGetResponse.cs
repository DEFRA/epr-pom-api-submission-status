namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionFileGet;

using Data.Enums;

public class SubmissionFileGetResponse
{
    public Guid SubmissionId { get; set; }

    public SubmissionType SubmissionType { get; set; }

    public Guid FileId { get; set; }

    public string FileName { get; set; }

    public FileType FileType { get; set; }

    public Guid UserId { get; set; }

    public Guid OrganisationId { get; set; }

    public string SubmissionPeriod { get; set; }

    public List<string> Errors { get; set; }
}