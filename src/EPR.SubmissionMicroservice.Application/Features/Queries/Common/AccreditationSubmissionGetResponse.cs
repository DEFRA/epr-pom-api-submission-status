using System.Diagnostics.CodeAnalysis;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

[ExcludeFromCodeCoverage]
public class AccreditationSubmissionGetResponse : AbstractSubmissionGetResponse
{
    public Guid FileId { get; set; }

    public string? AccreditationFileName { get; set; }

    public DateTime? AccreditationFileUploadDateTime { get; set; }

    public bool AccreditationDataComplete { get; set; }

    public override bool HasValidFile => true;
}
