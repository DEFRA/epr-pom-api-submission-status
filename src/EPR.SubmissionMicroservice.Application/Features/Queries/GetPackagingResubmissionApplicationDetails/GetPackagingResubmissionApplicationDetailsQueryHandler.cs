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
        : IRequestHandler<GetPackagingResubmissionApplicationDetailsQuery, ErrorOr<List<GetPackagingResubmissionApplicationDetailsResponse>>>
{
    public async Task<ErrorOr<List<GetPackagingResubmissionApplicationDetailsResponse>>> Handle(GetPackagingResubmissionApplicationDetailsQuery request, CancellationToken cancellationToken)
    {
        var responses = new List<GetPackagingResubmissionApplicationDetailsResponse>();

        var query = submissionQueryRepository
         .GetAll(x =>
             x.OrganisationId == request.OrganisationId &&
             x.SubmissionType == SubmissionType.Producer &&
             request.SubmissionPeriods.Contains(x.SubmissionPeriod) &&
            (request.ComplianceSchemeId == null || x.ComplianceSchemeId == request.ComplianceSchemeId));

        foreach (var submissionPeriod in request.SubmissionPeriods)
        {
            var submissions = await query.Where(x => x.SubmissionPeriod == submissionPeriod).OrderByDescending(x => x.Created).ToListAsync(cancellationToken);
            var response = await HandleHelper(submissions.FirstOrDefault(), cancellationToken);
            if (response != null)
            {
                responses.Add(response);
            }
        }

        return responses;
    }

    private static GetPackagingResubmissionApplicationDetailsResponse packagingDataResubmissionResponse(
        DateTime? latestPackagingDetailsCreatedDatetime,
        bool isFileUploadedButNotSubmittedYet,
        bool isRegulatorDecisionAfterSubmission,
        bool isResubmissionDoneAfterSubmission,
        GetPackagingResubmissionApplicationDetailsResponse response)
    {
        if ((latestPackagingDetailsCreatedDatetime == null) ||
            (!isRegulatorDecisionAfterSubmission && isResubmissionDoneAfterSubmission))
        {
            // Report that no cycle is open without erasing the cycle's identity. Replacing the response
            // here previously dropped ApplicationReferenceNumber and the declaration fields, which drove
            // the frontend to raise a second reference number for a cycle that already existed.
            response.ApplicationStatus = ApplicationStatusType.NotStarted;
            return response;
        }

        response.ApplicationStatus = isFileUploadedButNotSubmittedYet ? ApplicationStatusType.FileUploaded : ApplicationStatusType.SubmittedToRegulator;

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

    private async Task<GetPackagingResubmissionApplicationDetailsResponse> HandleHelper(Submission? submission, CancellationToken cancellationToken)
    {
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
            .Where(p => p.PaymentMethod != "Offline")
            .MaxBy(d => d.Created);

        var packagingFeeViewEvent = submissionEvents.OfType<PackagingResubmissionFeeViewCreatedEvent>()
            .MaxBy(d => d.Created);

        var packagingResubmissionReferenceNumberCreatedEvents = submissionEvents.OfType<PackagingResubmissionReferenceNumberCreatedEvent>()
            .OrderBy(d => d.Created)
            .ToList();

        var packagingApplicationSubmittedEvents = submissionEvents.OfType<PackagingResubmissionApplicationSubmittedCreatedEvent>()
            .Where(s => s.IsResubmitted == true)
            .ToList();

        if (packagingResubmissionReferenceNumberCreatedEvents.Count == 0)
        {
            return new GetPackagingResubmissionApplicationDetailsResponse()
            {
                SubmissionId = submission.Id,
                IsSubmitted = submission?.IsSubmitted ?? false,
            };
        }

        // The cycle is identified by the earliest reference-number event that has not been closed by a
        // later application-submitted event, so a duplicate reference number raised mid-cycle does not start
        // a new cycle. The cycle's own reference number is reported on every path below, including the ones
        // that report no progress, because it is what identifies the cycle to the frontend.
        var openCycleReferenceNumberEvent = GetOpenCycleReferenceNumberEvent(packagingResubmissionReferenceNumberCreatedEvents, packagingApplicationSubmittedEvents);
        var isCycleOpen = openCycleReferenceNumberEvent is not null;
        var packagingResubmissionReferenceNumberCreatedEvent = openCycleReferenceNumberEvent ?? packagingResubmissionReferenceNumberCreatedEvents[^1];

        if (latestPackagingDetailsAntivirusCheckEvent is null ||
            latestPackagingDetailsAntivirusCheckEvent.Created < packagingResubmissionReferenceNumberCreatedEvent.Created)
        {
            // Nothing has been uploaded since this cycle's reference number was created, so none of the
            // previous cycle's file, fee or declaration state belongs to it. The status stays NotStarted -
            // this cycle's upload step genuinely has not been started - while the reference number is still
            // reported, which is what tells the frontend the cycle exists and keeps the journey reachable.
            return new GetPackagingResubmissionApplicationDetailsResponse()
            {
                SubmissionId = submission.Id,
                IsSubmitted = submission?.IsSubmitted ?? false,
                ApplicationReferenceNumber = packagingResubmissionReferenceNumberCreatedEvent.PackagingResubmissionReferenceNumber,
                ApplicationStatus = ApplicationStatusType.NotStarted
            };
        }

        var validationPass = await IsValidationPass(submissionEvents, latestPackagingDetailsAntivirusCheckEvent, checkSplitterValidationEvents, producerValidationEvents, cancellationToken);
        var latestPackagingDetailsCreatedDatetime = validationPass ? latestPackagingDetailsAntivirusCheckEvent?.Created : null;
        var latestSubmittedEventCreatedDatetime = submittedEvent?.Created;
        var resubmissionEvent = packagingApplicationSubmittedEvents.MaxBy(x => x.SubmissionDate);

        var isFileUploadedButNotSubmittedYet = latestPackagingDetailsCreatedDatetime > latestSubmittedEventCreatedDatetime;
        var isRegulatorDecisionAfterSubmission = latestPackagingDetailsCreatedDatetime > (regulatorPackagingDecisionEvent?.Created ?? DateTime.MinValue);

        // A declaration closes the cycle it belongs to, so it may only be reported while nothing has started
        // a later one. Two things start a later cycle: a reference number raised after the declaration (an
        // open cycle), and a file submitted after it. The submit check is what stops a declaration being
        // reported for the rest of the submission's life - a reference number is only ever raised once per
        // submission, so cycle membership alone would mark every later cycle's declaration step complete
        // before the user had declared anything.
        var isDeclarationSupersededByLaterSubmit = resubmissionEvent is not null &&
                                                   latestSubmittedEventCreatedDatetime > resubmissionEvent.Created;
        var isResubmissionDoneAfterSubmission = !isCycleOpen && !isDeclarationSupersededByLaterSubmit;

        // SUB-345: a declaration the regulator has ruled on belongs to a closed cycle, so it must stop being
        // reported at the decision rather than at the next submit. Neither check above closes this window.
        // isCycleOpen cannot: a reference number is raised at most once per submission, and the frontend
        // will not raise a second one while this response still carries the first - which it must, so that
        // the cycle keeps its identity. isDeclarationSupersededByLaterSubmit cannot either, because the
        // ruled-on file was submitted before the declaration that closed its cycle. Left reported, the
        // frontend reads ResubmissionApplicationSubmitted as "declared, awaiting the regulator" and routes
        // past the page that shows the decision - the only place the regulator's comments are shown.
        //
        // Only the three outcomes that page can speak to count as a ruling. Cancelled, Queried and None
        // supersede nothing, so their declaration keeps being reported and the frontend keeps routing them
        // straight to the task list, as it did before this change. UploadNewFileToSubmit has no wording for
        // them: they fall through every decision branch to a bare "you already submitted a file", with the
        // regulator's comment never rendered, which is worse than not stopping there at all.
        //
        // This is deliberately kept out of isResubmissionDoneAfterSubmission, which also decides the status
        // below. Both stay true here so that the status branch keeps reporting NotStarted: the ruled-on
        // file's upload and fee belong to the closed cycle, and resolving them as completed would leave the
        // user with the upload step done and no way to replace the file the regulator rejected.
        var cycleClosingDecisionEvent = regulatorPackagingDecisionEvent?.Decision is RegulatorDecision.Accepted
                                            or RegulatorDecision.Approved
                                            or RegulatorDecision.Rejected
            ? regulatorPackagingDecisionEvent
            : null;

        var isDeclarationSupersededByRegulatorDecision = resubmissionEvent is not null &&
                                                         cycleClosingDecisionEvent?.Created > resubmissionEvent.Created;
        var shouldReportDeclaration = isResubmissionDoneAfterSubmission && !isDeclarationSupersededByRegulatorDecision;

        // SUB-345: the fee view and the fee payment belong to whichever cycle was open when they happened, so
        // a decision that closes a cycle has to age them out along with the declaration. Their only floor was
        // the submit, and for a ruled-on cycle the submit predates the declaration that closed it, so both
        // survived the decision and were reported against the untouched cycle that follows. The frontend read
        // that as "fee viewed, not yet declared" and told the user on the sub-landing tile to submit to the
        // regulator, while the task list - which keys off ApplicationStatus - correctly showed nothing started.
        //
        // The floor is the later of the two, not the decision alone: once a newer file is submitted, that
        // submit starts the cycle the fee events have to belong to.
        var currentCycleFloor = cycleClosingDecisionEvent is not null && cycleClosingDecisionEvent.Created > latestSubmittedEventCreatedDatetime
            ? cycleClosingDecisionEvent.Created
            : latestSubmittedEventCreatedDatetime;

        var isPackagingFeeViewEventInCurrentCycle = packagingFeeViewEvent?.Created > currentCycleFloor;
        var isPackagingFeePaymentEventInCurrentCycle = packagingFeePaymentEvent?.Created > currentCycleFloor;

        var response = new GetPackagingResubmissionApplicationDetailsResponse
        {
            SubmissionId = submission.Id,
            IsSubmitted = submission.IsSubmitted ?? false,
            ApplicationReferenceNumber = packagingResubmissionReferenceNumberCreatedEvent.PackagingResubmissionReferenceNumber,
            ResubmissionFeePaymentMethod = isPackagingFeePaymentEventInCurrentCycle ? packagingFeePaymentEvent?.PaymentMethod : null,
            LastSubmittedFile = !isFileUploadedButNotSubmittedYet
                ? new LastSubmittedFileDetails
                {
                    SubmittedDateTime = submittedEvent?.Created,
                    FileId = submittedEvent?.FileId,
                    SubmittedByName = submittedEvent?.SubmittedBy
                }
                : null,
            ResubmissionApplicationSubmittedDate = shouldReportDeclaration ? resubmissionEvent?.SubmissionDate : null,
            ResubmissionApplicationSubmittedComment = shouldReportDeclaration ? resubmissionEvent?.Comments : null,
            IsResubmitted = shouldReportDeclaration ? resubmissionEvent?.IsResubmitted : null,
            IsResubmissionFeeViewed = isPackagingFeeViewEventInCurrentCycle ? packagingFeeViewEvent?.IsPackagingResubmissionFeeViewed : null,
            ResubmissionReferenceNumber = isRegulatorDecisionAfterSubmission ? regulatorPackagingDecisionEvent?.RegistrationReferenceNumber : null,
        };

        // The status reports how far this cycle's upload has actually got, whether or not the cycle is open:
        // an upload that never produced a valid file has to leave the upload step startable, or the user has
        // no way to replace it. Keeping the cycle reachable is the reference number's job, not the status's.
        return packagingDataResubmissionResponse(latestPackagingDetailsCreatedDatetime, isFileUploadedButNotSubmittedYet, isRegulatorDecisionAfterSubmission, isResubmissionDoneAfterSubmission, response);
    }

    private static PackagingResubmissionReferenceNumberCreatedEvent? GetOpenCycleReferenceNumberEvent(
        List<PackagingResubmissionReferenceNumberCreatedEvent> referenceNumberCreatedEvents,
        List<PackagingResubmissionApplicationSubmittedCreatedEvent> applicationSubmittedEvents)
    {
        var latestApplicationSubmitted = applicationSubmittedEvents.Count > 0
            ? applicationSubmittedEvents.Max(x => x.Created)
            : (DateTime?)null;

        // referenceNumberCreatedEvents is ordered ascending, so the first match is the earliest cycle
        // that no application-submitted event has closed.
        return referenceNumberCreatedEvents
            .Find(x => latestApplicationSubmitted is null || x.Created > latestApplicationSubmitted);
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