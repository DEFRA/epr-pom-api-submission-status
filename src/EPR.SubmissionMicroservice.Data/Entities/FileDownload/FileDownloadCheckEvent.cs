using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.FileDownload;

[ExcludeFromCodeCoverage]
public class FileDownloadCheckEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.FileDownloadCheck;

    public string UserEmail { get; set; }

    public string ContentScan { get; set; }

    public Guid FileId { get; set; }

    public string FileName { get; set; }
}
