using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static EPR.SubmissionMicroservice.Application.Features.Queries.Common.GetPackagingResubmissionApplicationDetailsResponse;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;

public class GetPackagingResubmissionApplicationDetailsQueryHandler(
        IQueryRepository<Submission> submissionQueryRepository,
        IQueryRepository<AbstractSubmissionEvent> submissionEventQueryRepository,
        IQueryRepository<AbstractValidationError> validationErrorQueryRepository)
        : IRequestHandler<GetPackagingResubmissionApplicationDetailsQuery, ErrorOr<GetPackagingResubmissionApplicationDetailsResponse>>
{
    public async Task<ErrorOr<GetPackagingResubmissionApplicationDetailsResponse>> Handle(GetPackagingResubmissionApplicationDetailsQuery request, CancellationToken cancellationToken)
    {
        var query = submissionQueryRepository
         .GetAll(x =>
             x.OrganisationId == request.OrganisationId &&
             x.SubmissionType == SubmissionType.Producer &&
             x.SubmissionPeriod == request.SubmissionPeriod);

        if (request.ComplianceSchemeId is not null)
        {
            query = query.Where(x => x.ComplianceSchemeId == request.ComplianceSchemeId);
        }

        var submissions = await query.OrderByDescending(x => x.Created).ToListAsync(cancellationToken);
        var submission = submissions.FirstOrDefault();

        if (submission is null)
        {
            return default;
        }

        var submissionEvents = await submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submission.Id)
            .ToListAsync(cancellationToken);

        var submittedEvent = submissionEvents.OfType<SubmittedEvent>()
            .MaxBy(d => d.Created);

        var checkSplitterValidationEvents = submissionEvents.OfType<CheckSplitterValidationEvent>()
            .OrderByDescending(d => d.Created).ToList();

        var producerValidationEvents = submissionEvents.OfType<ProducerValidationEvent>()
            .OrderByDescending(d => d.Created).ToList();

        var regulatorPackagingDecisionEvent = submissionEvents.OfType<RegulatorPoMDecisionEvent>()
            .MaxBy(d => d.Created);

        var packagingFeePaymentEvent = submissionEvents.OfType<PackagingDataResubmissionFeePaymentEvent>()
            .Where(s => !string.IsNullOrWhiteSpace(s.ReferenceNumber))
            .MaxBy(d => d.Created);

        var packagingApplicationSubmittedEvents = submissionEvents.OfType<PackagingResubmissionApplicationSubmittedEvent>()
            .Where(s => !string.IsNullOrWhiteSpace(s.ApplicationReferenceNumber))
            .ToList();

        var latestPackagingDetailsAntivirusCheckEvent = submissionEvents.OfType<AntivirusCheckEvent>()
            .Where(x => x.FileType == FileType.Pom)
            .MaxBy(x => x.Created);

        var islatestUploadValid = false;
        var isProcessingComplete = false;

        if (latestPackagingDetailsAntivirusCheckEvent is not null)
        {
            var latestPackagingDetailsAntivirusResultEvent = submissionEvents.OfType<AntivirusResultEvent>()
               .Where(x => x.FileId == latestPackagingDetailsAntivirusCheckEvent.FileId)
               .MaxBy(x => x.Created);

            if (latestPackagingDetailsAntivirusResultEvent is not null)
            {
                if (checkSplitterValidationEvents.Count > 0)
                {
                    var latestUploadCheckSplitterEvent = checkSplitterValidationEvents.Find(x => x.BlobName == latestPackagingDetailsAntivirusResultEvent.BlobName);

                    if (latestUploadCheckSplitterEvent is not null)
                    {
                        var latestCheckerSplitterFuncErrors = latestUploadCheckSplitterEvent.Errors;
                        var latestValidationEvents = GetValidationEvents(checkSplitterValidationEvents, producerValidationEvents, latestUploadCheckSplitterEvent);
                        var latestValidationEventErrors = latestValidationEvents.Where(x => x.IsValid == false).ToList();
                        var latestUploadHasAllExpectedValidationEvents = latestUploadCheckSplitterEvent.DataCount == latestValidationEvents.Count(x => x.Type == EventType.ProducerValidation);

                        islatestUploadValid = latestUploadHasAllExpectedValidationEvents && latestCheckerSplitterFuncErrors.Count == 0 && latestValidationEventErrors.Count == 0;
                        isProcessingComplete = await IsProcessingComplete(latestUploadHasAllExpectedValidationEvents, latestValidationEvents, latestUploadCheckSplitterEvent, cancellationToken);
                    }
                }
            }
        }

        var validationPass = isProcessingComplete && islatestUploadValid;
        var latestPackagingDetailsCreatedDatetime = validationPass ? latestPackagingDetailsAntivirusCheckEvent?.Created : null;
        var latestSubmittedEventCreatedDatetime = submittedEvent?.Created;
        var isLatestSubmittedEventAfterFileUpload = latestSubmittedEventCreatedDatetime > latestPackagingDetailsCreatedDatetime;

        var packagingApplicationSubmittedEvent = packagingApplicationSubmittedEvents.MaxBy(x => x.SubmissionDate);

        var response = new GetPackagingResubmissionApplicationDetailsResponse
        {
            SubmissionId = submission.Id,
            IsSubmitted = submission.IsSubmitted ?? false,
            ApplicationReferenceNumber = submission.AppReferenceNumber,
            ResubmissionFeePaymentMethod = packagingFeePaymentEvent?.PaymentMethod,
            LastSubmittedFile = isLatestSubmittedEventAfterFileUpload
                ? new LastSubmittedFileDetails
                {
                    SubmittedDateTime = submittedEvent?.Created,
                    FileId = submittedEvent?.FileId,
                    SubmittedByName = submittedEvent?.SubmittedBy
                }
                : null,
            ResubmissionApplicationSubmittedDate = packagingApplicationSubmittedEvent?.SubmissionDate,
            ResubmissionApplicationSubmittedComment = packagingApplicationSubmittedEvent?.Comments,
            ResubmissionReferenceNumber = regulatorPackagingDecisionEvent?.RegistrationReferenceNumber
        };

        if (response.IsSubmitted)
        {
            response.ApplicationStatus = isLatestSubmittedEventAfterFileUpload
                ? ApplicationStatusType.SubmittedToRegulator
                : ApplicationStatusType.SubmittedAndHasRecentFileUpload;
        }
        else
        {
            response.ApplicationStatus = latestPackagingDetailsCreatedDatetime != null
                ? ApplicationStatusType.FileUploaded
                : ApplicationStatusType.NotStarted;
        }

        if (regulatorPackagingDecisionEvent is not null)
        {
            response.ApplicationStatus = regulatorPackagingDecisionEvent.Decision.ToString() switch
            {
                "Accepted" => ApplicationStatusType.AcceptedByRegulator,
                "Approved" => ApplicationStatusType.ApprovedByRegulator,
                "Rejected" => ApplicationStatusType.RejectedByRegulator,
                "Cancelled" => ApplicationStatusType.CancelledByRegulator,
                "Queried" => ApplicationStatusType.QueriedByRegulator
            };
        }

        if (response.ApplicationStatus is
                ApplicationStatusType.CancelledByRegulator
                or ApplicationStatusType.QueriedByRegulator
                or ApplicationStatusType.RejectedByRegulator
            && regulatorPackagingDecisionEvent.Created < latestPackagingDetailsCreatedDatetime)
        {
            response.ApplicationStatus = isLatestSubmittedEventAfterFileUpload
                ? ApplicationStatusType.SubmittedToRegulator
                : ApplicationStatusType.SubmittedAndHasRecentFileUpload;

            if (packagingFeePaymentEvent?.Created < regulatorPackagingDecisionEvent.Created)
            {
                response.ResubmissionFeePaymentMethod = null;
            }

            if (packagingApplicationSubmittedEvent?.Created < regulatorPackagingDecisionEvent.Created)
            {
                response.ResubmissionApplicationSubmittedComment = null;
                response.ResubmissionApplicationSubmittedDate = null;
            }
        }

        return response;
    }

    private async Task<bool> IsProcessingComplete(bool latestUploadHasAllExpectedValidationEvents, List<AbstractValidationEvent> latestValidationEvents, CheckSplitterValidationEvent latestUploadCheckSplitterEvent, CancellationToken cancellationToken)
    {
        var currentErrorCount = await GetProducerValidationErrorsCountByBlobNameAsync(latestUploadCheckSplitterEvent.BlobName, cancellationToken);
        var currentWarningCount = await GetProducerValidationWarningsCountByBlobNameAsync(latestUploadCheckSplitterEvent.BlobName, cancellationToken);

        var errorCountSum = latestValidationEvents.Sum(x => x.ErrorCount);
        var warningCountSum = latestValidationEvents.Sum(x => x.WarningCount);

        var hasEqualErrorCounts = currentErrorCount == errorCountSum;
        var hasEqualWarningCounts = currentWarningCount == warningCountSum;

        return latestUploadHasAllExpectedValidationEvents && hasEqualErrorCounts && hasEqualWarningCounts;
    }

    private async Task<int> GetProducerValidationWarningsCountByBlobNameAsync(string blobName, CancellationToken cancellationToken)
    {
        return await validationErrorQueryRepository
            .GetAll(x => x.BlobName == blobName)
            .Cast<AbstractValidationWarning>()
            .CountAsync(cancellationToken);
    }

    private async Task<int> GetProducerValidationErrorsCountByBlobNameAsync(string blobName, CancellationToken cancellationToken)
    {
        return await validationErrorQueryRepository
            .GetAll(x => x.BlobName == blobName)
            .Cast<AbstractValidationError>()
            .CountAsync(cancellationToken);
    }

    private List<AbstractValidationEvent> GetValidationEvents(List<CheckSplitterValidationEvent> checkSplitterValidationEvents, List<ProducerValidationEvent> producerValidationEvents, CheckSplitterValidationEvent latestUploadCheckSplitterEvent)
    {
        var latestValidationEvents = new List<AbstractValidationEvent>();

        latestValidationEvents.Add(checkSplitterValidationEvents.Find(x => x.BlobName == latestUploadCheckSplitterEvent.BlobName));
        latestValidationEvents.Add(producerValidationEvents.Find(x => x.BlobName == latestUploadCheckSplitterEvent.BlobName));

        return latestValidationEvents;
    }
}