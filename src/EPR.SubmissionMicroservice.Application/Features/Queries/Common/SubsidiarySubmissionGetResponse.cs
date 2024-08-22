namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubsidiarySubmissionGetResponse : AbstractSubmissionGetResponse
{
    public string? SubsidiaryFileName { get; set; }

    public bool SubsidiaryDataComplete { get; set; }

    public DateTime? SubsidiaryFileUploadDateTime { get; set; }

    public override bool HasValidFile => true;
}
