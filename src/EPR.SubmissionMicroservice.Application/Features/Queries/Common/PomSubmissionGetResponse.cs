namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class PomSubmissionGetResponse : AbstractSubmissionGetResponse
{
    public string? PomFileName { get; set; }

    public bool PomDataComplete { get; set; }

    public DateTime? PomFileUploadDateTime { get; set; }

    public UploadedPomFileInformation? LastUploadedValidFile { get; set; }

    public SubmittedPomFileInformation? LastSubmittedFile { get; set; }

    public override bool HasValidFile => LastUploadedValidFile is not null;
}