namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class UploadedRegistrationFilesInformation
{
    public Guid CompanyDetailsFileId { get; set; }

    public string CompanyDetailsFileName { get; set; }

    public Guid CompanyDetailsUploadedBy { get; set; }

    public DateTime CompanyDetailsUploadDatetime { get; set; }

    public string? BrandsFileName { get; set; }

    public Guid? BrandsUploadedBy { get; set; }

    public DateTime? BrandsUploadDatetime { get; set; }

    public string? PartnershipsFileName { get; set; }

    public Guid? PartnershipsUploadedBy { get; set; }

    public DateTime? PartnershipsUploadDatetime { get; set; }
}