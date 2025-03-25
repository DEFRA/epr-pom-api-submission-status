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
        IQueryRepository<AbstractValidationError> validationErrorQueryRepository,
        IQueryRepository<AbstractValidationWarning> validationWarningQueryRepository)
        : IRequestHandler<GetPackagingResubmissionApplicationDetailsQuery, ErrorOr<GetPackagingResubmissionApplicationDetailsResponse>>
{
    public async Task<ErrorOr<GetPackagingResubmissionApplicationDetailsResponse>> Handle(GetPackagingResubmissionApplicationDetailsQuery request, CancellationToken cancellationToken)
    {
        var query = GetSubmissionQuery(request);

        var submissions = await query.OrderByDescending(x => x.Created).ToListAsync(cancellationToken);
        var submission = submissions.Find(x => !string.IsNullOrEmpty(x.AppReferenceNumber));

        if (submission is null)
        {
            return default;
        }

        var submissionEvents = await submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submission.Id)
            .ToListAsync(cancellationToken);

        var latestPackagingDetailsAntivirusCheckEvent = submissionEvents.OfType<AntivirusCheckEvent>()
            .Where(x => x.FileType == FileType.Pom)
            .MaxBy(x => x.Created);

        var checkSplitterValidationEvents = submissionEvents.OfType<CheckSplitterValidationEvent>()
            .OrderByDescending(d => d.Created).ToList();

        var producerValidationEvents = submissionEvents.OfType<ProducerValidationEvent>()
            .OrderByDescending(d => d.Created).ToList();

        var submittedEvent = submissionEvents.OfType<SubmittedEvent>()
            .MaxBy(d => d.Created);

        var regulatorPackagingDecisionEvent = submissionEvents.OfType<RegulatorPoMDecisionEvent>()
            .MaxBy(d => d.Created);

        var packagingFeePaymentEvent = submissionEvents.OfType<PackagingDataResubmissionFeePaymentEvent>()
            .MaxBy(d => d.Created);

        var packagingFeeViewEvent = submissionEvents.OfType<PackagingResubmissionFeeViewCreatedEvent>()
            .MaxBy(d => d.Created);

        var packagingResubmissionReferenceNumberCreatedEvent = submissionEvents.OfType<PackagingResubmissionReferenceNumberCreatedEvent>()
            .MaxBy(d => d.Created);

        var packagingApplicationSubmittedEvents = submissionEvents.OfType<PackagingResubmissionApplicationSubmittedCreatedEvent>()
            .Where(s => (bool)s.IsResubmitted)
            .ToList();

        if (packagingResubmissionReferenceNumberCreatedEvent is null)
        {
            return new GetPackagingResubmissionApplicationDetailsResponse()
            {
                SubmissionId = submission.Id,
                IsSubmitted = submission?.IsSubmitted ?? false,
                ApplicationReferenceNumber = submission.AppReferenceNumber
            };
        }

        if (latestPackagingDetailsAntivirusCheckEvent.Created < packagingResubmissionReferenceNumberCreatedEvent.Created)
        {
            return new GetPackagingResubmissionApplicationDetailsResponse()
            {
                SubmissionId = submission.Id,
                IsSubmitted = submission?.IsSubmitted ?? false,
                ApplicationReferenceNumber = submission.AppReferenceNumber
            };
        }

        var validationPass = await IsValidationPass(submissionEvents, latestPackagingDetailsAntivirusCheckEvent, checkSplitterValidationEvents, producerValidationEvents, cancellationToken);
        var latestPackagingDetailsCreatedDatetime = validationPass ? latestPackagingDetailsAntivirusCheckEvent?.Created : null;
        var latestSubmittedEventCreatedDatetime = submittedEvent?.Created;
        var packagingApplicationSubmittedEvent = packagingApplicationSubmittedEvents.MaxBy(x => x.SubmissionDate);

        var isFileUploadedButNotSubmittedYet = latestPackagingDetailsCreatedDatetime > latestSubmittedEventCreatedDatetime;
        var isRegulatorDecisionAfterSubmission = latestPackagingDetailsCreatedDatetime > (regulatorPackagingDecisionEvent?.Created ?? DateTime.MinValue);
        var isApplicationSubmittedAfterSubmission = packagingApplicationSubmittedEvent?.Created > latestSubmittedEventCreatedDatetime;
        var isPackagingFeeViewEventAfterSubmission = packagingFeeViewEvent?.Created > latestSubmittedEventCreatedDatetime;
        var isPackagingFeePaymentEventAfterSubmission = packagingFeePaymentEvent?.Created > latestSubmittedEventCreatedDatetime;

        var response = new GetPackagingResubmissionApplicationDetailsResponse
        {
            SubmissionId = submission.Id,
            IsSubmitted = submission.IsSubmitted ?? false,
            ApplicationReferenceNumber = submission.AppReferenceNumber,
            ResubmissionFeePaymentMethod = isPackagingFeePaymentEventAfterSubmission ? packagingFeePaymentEvent?.PaymentMethod : null,
            LastSubmittedFile = !isFileUploadedButNotSubmittedYet
                ? new LastSubmittedFileDetails
                {
                    SubmittedDateTime = submittedEvent?.Created,
                    FileId = submittedEvent?.FileId,
                    SubmittedByName = submittedEvent?.SubmittedBy
                }
                : null,
            ResubmissionApplicationSubmittedDate = isApplicationSubmittedAfterSubmission ? packagingApplicationSubmittedEvent?.SubmissionDate : null,
            ResubmissionApplicationSubmittedComment = isApplicationSubmittedAfterSubmission ? packagingApplicationSubmittedEvent?.Comments : null,
            IsResubmitted = isApplicationSubmittedAfterSubmission ? packagingApplicationSubmittedEvent?.IsResubmitted : null,
            IsResubmissionFeeViewed = isPackagingFeeViewEventAfterSubmission ? packagingFeeViewEvent?.IsPackagingResubmissionFeeViewed : null,
            ResubmissionReferenceNumber = isRegulatorDecisionAfterSubmission ? regulatorPackagingDecisionEvent?.RegistrationReferenceNumber : null,
        };

        return packagingDataResubmissionResponse(submission, latestPackagingDetailsCreatedDatetime, isFileUploadedButNotSubmittedYet, isRegulatorDecisionAfterSubmission, isApplicationSubmittedAfterSubmission, response);
    }

    private static GetPackagingResubmissionApplicationDetailsResponse packagingDataResubmissionResponse(Submission? submission, DateTime? latestPackagingDetailsCreatedDatetime, bool isFileUploadedButNotSubmittedYet, bool isRegulatorDecisionAfterSubmission, bool isApplicationSubmittedAfterSubmission, GetPackagingResubmissionApplicationDetailsResponse response)
    {
        if (isFileUploadedButNotSubmittedYet)
        {
            response.ApplicationStatus = latestPackagingDetailsCreatedDatetime != null
                ? ApplicationStatusType.FileUploaded
                : ApplicationStatusType.NotStarted;
        }
        else
        {
            response.ApplicationStatus = ApplicationStatusType.SubmittedToRegulator;
        }

        if (!isRegulatorDecisionAfterSubmission && isApplicationSubmittedAfterSubmission)
        {
            response = new GetPackagingResubmissionApplicationDetailsResponse();
            response.SubmissionId = submission.Id;
            response.IsSubmitted = submission.IsSubmitted ?? false;
            response.ApplicationReferenceNumber = submission.AppReferenceNumber;
            response.ApplicationStatus = ApplicationStatusType.NotStarted;
        }

        return response;
    }

    private static List<AbstractValidationEvent> GetValidationEvents(List<CheckSplitterValidationEvent> checkSplitterValidationEvents, List<ProducerValidationEvent> producerValidationEvents, CheckSplitterValidationEvent latestUploadCheckSplitterEvent)
    {
        var latestValidationEvents = new List<AbstractValidationEvent>();

        var checkSplitterValidationEventsList = checkSplitterValidationEvents.Where(x => x.BlobName == latestUploadCheckSplitterEvent.BlobName).ToList();
        var producerValidationEventsList = producerValidationEvents.Where(x => x.BlobName == latestUploadCheckSplitterEvent.BlobName).ToList();

        latestValidationEvents.AddRange(checkSplitterValidationEventsList);
        latestValidationEvents.AddRange(producerValidationEventsList);

        return latestValidationEvents;
    }

    private async Task<bool> IsValidationPass(List<AbstractSubmissionEvent> submissionEvents, AntivirusCheckEvent? latestPackagingDetailsAntivirusCheckEvent, List<CheckSplitterValidationEvent> checkSplitterValidationEvents, List<ProducerValidationEvent> producerValidationEvents, CancellationToken cancellationToken)
    {
        var islatestUploadValid = false;
        var isProcessingComplete = false;

        if (latestPackagingDetailsAntivirusCheckEvent is not null)
        {
            var latestPackagingDetailsAntivirusResultEvent = submissionEvents.OfType<AntivirusResultEvent>()
               .Where(x => x.FileId == latestPackagingDetailsAntivirusCheckEvent.FileId)
               .MaxBy(x => x.Created);

            if (latestPackagingDetailsAntivirusResultEvent is not null && checkSplitterValidationEvents.Count > 0)
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

        var validationPass = isProcessingComplete && islatestUploadValid;
        return validationPass;
    }

    private IQueryable<Submission> GetSubmissionQuery(GetPackagingResubmissionApplicationDetailsQuery request)
    {
        return submissionQueryRepository
          .GetAll(x =>
              x.OrganisationId == request.OrganisationId &&
              x.SubmissionType == SubmissionType.Producer &&
              x.SubmissionPeriod == request.SubmissionPeriod &&
             (request.ComplianceSchemeId == null || x.ComplianceSchemeId == request.ComplianceSchemeId));
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
        return await validationWarningQueryRepository
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
}