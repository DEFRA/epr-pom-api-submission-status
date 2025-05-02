namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

public class SubmittedRegistrationFilesInformation
{
    public Guid CompanyDetailsFileId { get; set; }

    public string CompanyDetailsFileName { get; set; }

    public string? BrandsFileName { get; set; }

    public string? PartnersFileName { get; set; }

    public DateTime SubmittedDateTime { get; set; }

    public Guid SubmittedBy { get; set; }
}