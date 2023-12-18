namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubmittedPomFileInformation
{
    public Guid FileId { get; set; }

    public string FileName { get; set; }

    public DateTime SubmittedDateTime { get; set; }

    public Guid? SubmittedBy { get; set; }
}