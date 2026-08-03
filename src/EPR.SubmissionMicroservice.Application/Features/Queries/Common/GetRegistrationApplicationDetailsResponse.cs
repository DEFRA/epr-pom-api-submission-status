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

    public bool HasAnyApprovedOrQueriedRegulatorDecision { get; set; }

    public bool IsLatestSubmittedEventAfterFileUpload { get; set; }

    public DateTime? LatestSubmittedEventCreatedDatetime { get; set; }

    public DateTime? FirstApplicationSubmittedEventCreatedDatetime { get; set; }

    public string? RegistrationJourney { get; set; }

    // Blob name of the latest company-details file that reached the antivirus check (the file the current
    // fee snapshot in the payment-service DB should correspond to). Used by the frontend to cross-check
    // fee responses so it doesn't display fees derived from a previous, stale snapshot.
    public string? LastUploadedFileBlobName { get; set; }

    public class LastSubmittedFileDetails
    {
        public Guid? FileId { get; set; }

        public string? SubmittedByName { get; set; } = string.Empty;

        public DateTime? SubmittedDateTime { get; set; }
    }
}