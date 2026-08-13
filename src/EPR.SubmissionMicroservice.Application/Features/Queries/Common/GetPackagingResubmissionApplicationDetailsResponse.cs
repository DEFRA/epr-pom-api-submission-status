using System.Diagnostics.CodeAnalysis;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common;

[ExcludeFromCodeCoverage]
public class GetPackagingResubmissionApplicationDetailsResponse
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

    public bool? IsResubmitted { get; set; }

    public bool? IsResubmissionFeeViewed { get; set; }

    public string? ApplicationReferenceNumber { get; set; } = string.Empty;

    public LastSubmittedFileDetails? LastSubmittedFile { get; set; }

    public string? ResubmissionFeePaymentMethod { get; set; }

    public DateTime? ResubmissionApplicationSubmittedDate { get; set; }

    public string? ResubmissionApplicationSubmittedComment { get; set; }

    public bool ResubmissionApplicationSubmitted => ResubmissionApplicationSubmittedDate is not null;

    public ApplicationStatusType ApplicationStatus { get; set; }

    public string? ResubmissionReferenceNumber { get; set; }

    /// <summary>
    /// SUB-345: the most recent resubmission cycle the regulator has already ruled on, or null if there is none.
    /// </summary>
    /// <remarks>
    /// Every field above describes the cycle that is open now, so all of them stop reporting a cycle at the
    /// decision that closed it. That is right for "what is outstanding" and wrong for "what has been done":
    /// on its own it leaves a completed resubmission indistinguishable from one that was never started. This
    /// carries the closed cycle instead of discarding it, without changing what any existing field means.
    /// </remarks>
    public CompletedResubmissionDetails? LastCompletedResubmission { get; set; }

    public class LastSubmittedFileDetails
    {
        public Guid? FileId { get; set; }

        public string? SubmittedByName { get; set; } = string.Empty;

        public DateTime? SubmittedDateTime { get; set; }
    }

    public class CompletedResubmissionDetails
    {
        public string? ApplicationReferenceNumber { get; set; }

        public string? ResubmissionReferenceNumber { get; set; }

        public DateTime? DeclarationDate { get; set; }

        public string? DeclarationComment { get; set; }

        public string? DeclaredByName { get; set; }

        public bool? IsResubmissionFeeViewed { get; set; }

        public string? ResubmissionFeePaymentMethod { get; set; }

        public string? Decision { get; set; }

        public string? RegulatorComments { get; set; }

        public DateTime? DecisionDate { get; set; }

        public string? FileName { get; set; }

        public LastSubmittedFileDetails? SubmittedFile { get; set; }
    }
}