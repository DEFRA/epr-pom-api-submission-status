using System.Diagnostics.CodeAnalysis;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

[ExcludeFromCodeCoverage]
public class GetRegistrationApplicationDetailsResponse
{
    public enum ApplicationStatusType
    {
        NotStarted,
        FileUploaded,
        SubmittedAndHasRecentFileUpload,
        SubmittedToRegulator,
        AcceptedByRegulator,
        RejectedByRegulator,
        ApprovedByRegulator,
        CancelledByRegulator,
        QueriedByRegulator
    }

    public Guid? SubmissionId { get; set; }

    public bool IsSubmitted { get; set; }

    public bool? IsResubmission { get; set; }

    public string? ApplicationReferenceNumber { get; set; } = string.Empty;

    public LastSubmittedFileDetails? LastSubmittedFile { get; set; }

    public string? RegistrationFeePaymentMethod { get; set; }

    public DateTime? RegistrationApplicationSubmittedDate { get; set; }

    public string? RegistrationApplicationSubmittedComment { get; set; }

    public bool RegistrationApplicationSubmitted => RegistrationApplicationSubmittedDate is not null;

    public ApplicationStatusType ApplicationStatus { get; set; }

    public string? RegistrationReferenceNumber { get; set; }

    public bool IsLateFeeApplicable { get; set; }

    public class LastSubmittedFileDetails
    {
        public Guid? FileId { get; set; }

        public string? SubmittedByName { get; set; } = string.Empty;

        public DateTime? SubmittedDateTime { get; set; }
    }
}