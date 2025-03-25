namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class PomSubmissionGetResponse : AbstractSubmissionGetResponse
{
    public string? PomFileName { get; set; }

    public bool PomDataComplete { get; set; }

    public DateTime? PomFileUploadDateTime { get; set; }

    public string? AppReferenceNumber { get; set; }

    public bool? IsResubmissionInProgress { get; set; }

    public bool? IsResubmissionComplete { get; set; }

    public UploadedPomFileInformation? LastUploadedValidFile { get; set; }

    public SubmittedPomFileInformation? LastSubmittedFile { get; set; }

    public override bool HasValidFile => LastUploadedValidFile is not null;
}