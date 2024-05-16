using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class FileDownloadCheckEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.FileDownloadCheck;

    public string UserEmail { get; set; }

    public string ContentScan { get; set; }

    public Guid FileId { get; set; }

    public string FileName { get; set; }

    public SubmissionType SubmissionType { get; set; }
}
