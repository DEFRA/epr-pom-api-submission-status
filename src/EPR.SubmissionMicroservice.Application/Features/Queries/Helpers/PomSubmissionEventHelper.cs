using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;

public class PomSubmissionEventHelper : IPomSubmissionEventHelper
{
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionEventQueryRepository;
    private readonly IQueryRepository<AbstractValidationWarning> _validationWarningQueryRepository;
    private readonly IQueryRepository<AbstractValidationError> _validationErrorQueryRepository;

    public PomSubmissionEventHelper(
        IQueryRepository<AbstractSubmissionEvent> submissionEventQueryRepository,
        IQueryRepository<AbstractValidationWarning> validationWarningQueryRepository,
        IQueryRepository<AbstractValidationError> validationErrorQueryRepository)
    {
        _submissionEventQueryRepository = submissionEventQueryRepository;
        _validationWarningQueryRepository = validationWarningQueryRepository;
        _validationErrorQueryRepository = validationErrorQueryRepository;
    }

    public async Task SetValidationEventsAsync(PomSubmissionGetResponse response, bool isSubmitted, CancellationToken cancellationToken)
    {
        var submissionId = response.Id;
        var latestAntivirusCheckEvent = await GetLatestAntivirusCheckEventAsync(submissionId, cancellationToken);

        if (latestAntivirusCheckEvent is null)
        {
            return;
        }

        var latestAntivirusResultEvent = await GetAntivirusResultEventByFileIdAsync(latestAntivirusCheckEvent.FileId, cancellationToken);
        var latestFileUploadErrors = new List<string>();
        var checkSplitterEvents = await GetCheckSplitterEventsAsync(submissionId, cancellationToken);
        var processingComplete = false;
        var validationPass = false;
        var hasWarningsInFile = false;

        if (latestAntivirusResultEvent is not null && latestAntivirusResultEvent.Errors.Count > 0)
        {
            latestFileUploadErrors.AddRange(latestAntivirusResultEvent.Errors);
        }

        if (checkSplitterEvents.Count > 0)
        {
            var latestUploadIsValid = false;
            var latestUploadCheckSplitterEvent = latestAntivirusResultEvent is not null
                ? checkSplitterEvents.Find(x => x.BlobName == latestAntivirusResultEvent.BlobName)
                : null;

            if (latestUploadCheckSplitterEvent is not null)
            {
                if (latestUploadCheckSplitterEvent.Errors.Count > 0)
                {
                    latestFileUploadErrors.AddRange(latestUploadCheckSplitterEvent.Errors);
                }

                var latestValidationEvents =
                    await GetValidationEventsByBlobNameAsync(latestUploadCheckSplitterEvent.BlobName, cancellationToken);
                var latestProducerValidationCount = latestValidationEvents
                    .Count(x => x.Type == EventType.ProducerValidation);
                var latestValidationEventErrors = latestValidationEvents
                    .Where(x => x.IsValid == false)
                    .ToList();
                var latestUploadHasAllExpectedValidationEvents =
                    latestUploadCheckSplitterEvent.DataCount == latestProducerValidationCount;

                var currentErrorCount = await GetProducerValidationErrorsCountByBlobNameAsync(latestUploadCheckSplitterEvent.BlobName, cancellationToken);
                var currentWarningCount = await GetProducerValidationWarningsCountByBlobNameAsync(latestUploadCheckSplitterEvent.BlobName, cancellationToken);

                var errorCountSum = latestValidationEvents.Sum(x => x.ErrorCount);
                var warningCountSum = latestValidationEvents.Sum(x => x.WarningCount);

                hasWarningsInFile = warningCountSum > 0;

                var hasEqualErrorCounts = currentErrorCount == errorCountSum;
                var hasEqualWarningCounts = currentWarningCount == warningCountSum;

                latestUploadIsValid = latestUploadHasAllExpectedValidationEvents && latestValidationEventErrors.Count == 0 && latestFileUploadErrors.Count == 0;
                processingComplete = latestUploadHasAllExpectedValidationEvents && hasEqualErrorCounts && hasEqualWarningCounts;
                validationPass = processingComplete && latestUploadIsValid;

                if (latestValidationEventErrors.Count > 0)
                {
                    latestFileUploadErrors.AddRange(latestValidationEventErrors.SelectMany(x => x.Errors));
                }
            }

            var latestValidFile = latestUploadIsValid
                ? await GetAntivirusCheckEventByBlobNameAsync(latestUploadCheckSplitterEvent.BlobName, cancellationToken)
                : await GetLatestValidFileFromCheckSplitterEventsAsync(checkSplitterEvents.Where(x => x.Id != latestUploadCheckSplitterEvent?.Id).ToList(), cancellationToken);

            if (latestValidFile is not null)
            {
                response.LastUploadedValidFile = new UploadedPomFileInformation
                {
                    FileName = latestValidFile.FileName,
                    FileUploadDateTime = latestValidFile.Created,
                    UploadedBy = latestValidFile.UserId,
                    FileId = latestValidFile.FileId
                };
            }

            if (isSubmitted)
            {
                var latestSubmittedEvent = await GetLatestSubmittedEventAsync(submissionId, cancellationToken);
                var submittedFileAntivirusCheckEvent = latestSubmittedEvent.FileId == latestValidFile?.FileId
                    ? latestValidFile
                    : await GetAntivirusCheckEventByFileIdAsync(submissionId, latestSubmittedEvent.FileId, cancellationToken);

                response.LastSubmittedFile = new SubmittedPomFileInformation
                {
                    SubmittedDateTime = latestSubmittedEvent.Created,
                    SubmittedBy = latestSubmittedEvent.UserId,
                    FileName = submittedFileAntivirusCheckEvent.FileName,
                    FileId = submittedFileAntivirusCheckEvent.FileId
                };

                await IsResubmissionInProgress(latestValidFile, latestSubmittedEvent, response, cancellationToken);
            }
        }

        response.PomFileName = latestAntivirusCheckEvent.FileName;
        response.PomFileUploadDateTime = latestAntivirusCheckEvent.Created;
        response.PomDataComplete = processingComplete;
        response.ValidationPass = validationPass;
        response.Errors = latestFileUploadErrors;
        response.HasWarnings = hasWarningsInFile;
    }

    public async Task<bool> VerifyFileIdIsForValidFileAsync(Guid submissionId, Guid fileId, CancellationToken cancellationToken)
    {
        var antivirusResultEvent = await GetAntivirusResultEventByFileIdAsync(fileId, cancellationToken);

        if (antivirusResultEvent is null)
        {
            return false;
        }

        var checkSplitterEvent = await _submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submissionId && x.Type == EventType.CheckSplitter && x.BlobName == antivirusResultEvent.BlobName)
            .Cast<CheckSplitterValidationEvent>()
            .FirstOrDefaultAsync(cancellationToken);

        if (checkSplitterEvent is null)
        {
            return false;
        }

        return await VerifyBlobNameHasExpectedNumberOfValidProducerValidationEventsAsync(checkSplitterEvent.BlobName, checkSplitterEvent.DataCount, cancellationToken);
    }

    public async Task<PomSubmissionGetResponse> IsResubmissionInProgress(AntivirusCheckEvent? latestValidFile, SubmittedEvent latestSubmittedEvent, PomSubmissionGetResponse response, CancellationToken cancellationToken)
    {
        response.IsResubmissionInProgress = false;

        if (string.IsNullOrEmpty(response.AppReferenceNumber))
        {
            return response;
        }

        response.IsResubmissionInProgress = true;

        if (latestValidFile.Created > latestSubmittedEvent.Created)
        {
            return response;
        }

        var packagingResubmissionApplicationSubmittedResponse = await GetPackagingResubmissionApplicationSubmitted(response.Id, cancellationToken);

        if (packagingResubmissionApplicationSubmittedResponse == null)
        {
            return response;
        }

        if (packagingResubmissionApplicationSubmittedResponse.Created < latestSubmittedEvent.Created)
        {
            return response;
        }

        response.IsResubmissionInProgress = false;
        response.IsResubmissionComplete = true;
        return response;
    }

    private async Task<PackagingResubmissionApplicationSubmittedCreatedEvent?> GetPackagingResubmissionApplicationSubmitted(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        return await _submissionEventQueryRepository
        .GetAll(x => x.SubmissionId == submissionId && x.Type == EventType.PackagingResubmissionApplicationSubmitted)
        .OrderByDescending(x => x.Created)
        .Cast<PackagingResubmissionApplicationSubmittedCreatedEvent>()
        .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AntivirusCheckEvent?> GetLatestAntivirusCheckEventAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        return await _submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submissionId && x.Type == EventType.AntivirusCheck)
            .OrderByDescending(x => x.Created)
            .Cast<AntivirusCheckEvent>()
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AntivirusCheckEvent?> GetLatestValidFileFromCheckSplitterEventsAsync(List<CheckSplitterValidationEvent> events, CancellationToken cancellationToken)
    {
        foreach (var checkSplitterEvent in events.Where(x => x.DataCount > 0).OrderByDescending(x => x.Created))
        {
            if (await VerifyBlobNameHasExpectedNumberOfValidProducerValidationEventsAsync(checkSplitterEvent.BlobName, checkSplitterEvent.DataCount, cancellationToken))
            {
                return await GetAntivirusCheckEventByBlobNameAsync(checkSplitterEvent.BlobName, cancellationToken);
            }
        }

        return null;
    }

    private async Task<bool> VerifyBlobNameHasExpectedNumberOfValidProducerValidationEventsAsync(string blobName, int expectedDataCount, CancellationToken cancellationToken)
    {
        return await _submissionEventQueryRepository
            .GetAll(x => x.Type == EventType.ProducerValidation && x.BlobName == blobName)
            .Cast<ProducerValidationEvent>()
            .Where(x => x.IsValid == true)
            .CountAsync(cancellationToken) == expectedDataCount;
    }

    private async Task<AntivirusCheckEvent> GetAntivirusCheckEventByBlobNameAsync(string blobName, CancellationToken cancellationToken)
    {
        var antivirusResultEvent = await _submissionEventQueryRepository
            .GetAll(x => x.BlobName == blobName && x.Type == EventType.AntivirusResult)
            .Cast<AntivirusResultEvent>()
            .FirstOrDefaultAsync(cancellationToken);

        if (antivirusResultEvent == null)
        {
            return null;
        }

        return await _submissionEventQueryRepository
            .GetAll(x => x.Type == EventType.AntivirusCheck)
            .Cast<AntivirusCheckEvent>()
            .Where(x => x.FileId == antivirusResultEvent.FileId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<SubmittedEvent> GetLatestSubmittedEventAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        return await _submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submissionId && x.Type == EventType.Submitted)
            .OrderByDescending(x => x.Created)
            .Cast<SubmittedEvent>()
            .FirstAsync(cancellationToken);
    }

    private async Task<AntivirusCheckEvent> GetAntivirusCheckEventByFileIdAsync(
        Guid submissionId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        return await _submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submissionId && x.Type == EventType.AntivirusCheck)
            .Cast<AntivirusCheckEvent>()
            .Where(x => x.FileId == fileId)
            .FirstAsync(cancellationToken);
    }

    private async Task<AntivirusResultEvent?> GetAntivirusResultEventByFileIdAsync(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        return await _submissionEventQueryRepository
            .GetAll(x => x.Type == EventType.AntivirusResult)
            .Cast<AntivirusResultEvent>()
            .Where(x => x.FileId == fileId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<List<CheckSplitterValidationEvent>> GetCheckSplitterEventsAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        return await _submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submissionId && x.Type == EventType.CheckSplitter)
            .OrderByDescending(x => x.Created)
            .Cast<CheckSplitterValidationEvent>()
            .ToListAsync(cancellationToken);
    }

    private async Task<List<AbstractValidationEvent>> GetValidationEventsByBlobNameAsync(
        string blobName,
        CancellationToken cancellationToken)
    {
        return await _submissionEventQueryRepository
            .GetAll(x => (x.Type == EventType.ProducerValidation || x.Type == EventType.CheckSplitter) && x.BlobName == blobName)
            .Cast<AbstractValidationEvent>()
            .ToListAsync(cancellationToken);
    }

    private async Task<int> GetProducerValidationWarningsCountByBlobNameAsync(string blobName, CancellationToken cancellationToken)
    {
        return await _validationWarningQueryRepository
            .GetAll(x => x.BlobName == blobName)
            .Cast<AbstractValidationWarning>()
            .CountAsync(cancellationToken);
    }

    private async Task<int> GetProducerValidationErrorsCountByBlobNameAsync(string blobName, CancellationToken cancellationToken)
    {
        return await _validationErrorQueryRepository
            .GetAll(x => x.BlobName == blobName)
            .Cast<AbstractValidationError>()
            .CountAsync(cancellationToken);
    }
}