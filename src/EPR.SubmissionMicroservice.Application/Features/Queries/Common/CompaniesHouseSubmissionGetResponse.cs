namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class CompaniesHouseSubmissionGetResponse : AbstractSubmissionGetResponse
{
    public string? CompaniesHouseFileName { get; set; }

    public bool CompaniesHouseDataComplete { get; set; }

    public DateTime? CompaniesHouseFileUploadDateTime { get; set; }

    public override bool HasValidFile => true;
}
