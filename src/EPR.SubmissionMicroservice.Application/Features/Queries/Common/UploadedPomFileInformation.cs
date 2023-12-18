namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class UploadedPomFileInformation
{
    public string FileName { get; set; }

    public Guid FileId { get; set; }

    public DateTime FileUploadDateTime { get; set; }

    public Guid UploadedBy { get; set; }
}