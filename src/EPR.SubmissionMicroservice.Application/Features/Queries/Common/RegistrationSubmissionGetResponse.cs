namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class RegistrationSubmissionGetResponse : AbstractSubmissionGetResponse
{
    public bool RequiresBrandsFile { get; set; }

    public bool RequiresPartnershipsFile { get; set; }

    public string? CompanyDetailsFileName { get; set; }

    public bool CompanyDetailsDataComplete { get; set; }

    public Guid? CompanyDetailsUploadedBy { get; set; }

    public DateTime? CompanyDetailsUploadedDate { get; set; }

    public string? BrandsFileName { get; set; }

    public bool BrandsDataComplete { get; set; }

    public Guid? BrandsUploadedBy { get; set; }

    public DateTime? BrandsUploadedDate { get; set; }

    public string? PartnershipsFileName { get; set; }

    public bool PartnershipsDataComplete { get; set; }

    public Guid? PartnershipsUploadedBy { get; set; }

    public DateTime? PartnershipsUploadedDate { get; set; }

    public UploadedRegistrationFilesInformation? LastUploadedValidFiles { get; set; }

    public SubmittedRegistrationFilesInformation? LastSubmittedFiles { get; set; }

    public override bool HasValidFile => LastUploadedValidFiles is not null;

    public bool HasMaxRowErrors { get; set; }

    public int RowErrorCount { get; set; } = 0;

    public bool CompanyDetailsFileIsValid { get; set; }

    public bool BrandsDataIsValid { get; set; }

    public bool PartnersDataIsValid { get; set; }

    public int? OrganisationMemberCount { get; set; }
}