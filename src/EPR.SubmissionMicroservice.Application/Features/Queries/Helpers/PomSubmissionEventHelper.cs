using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;

public class PomSubmissionEventHelper : IPomSubmissionEventHelper
{
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionEventQueryRepository;

    public PomSubmissionEventHelper(
        IQueryRepository<AbstractSubmissionEvent> submissionEventQueryRepository)
    {
        _submissionEventQueryRepository = submissionEventQueryRepository;
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

        if (latestAntivirusResultEvent is not null && latestAntivirusResultEvent.Errors.Any())
        {
            latestFileUploadErrors.AddRange(latestAntivirusResultEvent.Errors);
        }

        if (checkSplitterEvents.Any())
        {
            var latestUploadIsValid = false;
            var latestUploadCheckSplitterEvent = latestAntivirusResultEvent is not null
                ? checkSplitterEvents.Find(x => x.BlobName == latestAntivirusResultEvent.BlobName)
                : null;

            if (latestUploadCheckSplitterEvent is not null)
            {
                if (latestUploadCheckSplitterEvent.Errors.Any())
                {
                    latestFileUploadErrors.AddRange(latestUploadCheckSplitterEvent.Errors);
                }

                var latestProducerValidationCount =
                    await GetProducerValidationEventCountByBlobNameAsync(latestUploadCheckSplitterEvent.BlobName, cancellationToken);
                var latestProducerValidationErrors =
                    await GetInvalidProducerValidationEventsByBlobNameAsync(latestUploadCheckSplitterEvent.BlobName, cancellationToken);
                var latestUploadHasAllExpectedValidationEvents =
                    latestUploadCheckSplitterEvent.DataCount == latestProducerValidationCount;
                latestUploadIsValid = latestUploadHasAllExpectedValidationEvents && !latestProducerValidationErrors.Any() && !latestFileUploadErrors.Any();
                processingComplete = latestUploadHasAllExpectedValidationEvents;
                validationPass = processingComplete && latestUploadIsValid;
                hasWarningsInFile =
                    await BlobHasWarningsAsync(latestUploadCheckSplitterEvent.BlobName, cancellationToken);

                if (latestProducerValidationErrors.Any())
                {
                    latestFileUploadErrors.AddRange(latestProducerValidationErrors.SelectMany(x => x.Errors));
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
                var submittedFileAntivirusCheckEvent = latestSubmittedEvent.FileId == latestValidFile!.FileId
                    ? latestValidFile
                    : await GetAntivirusCheckEventByFileIdAsync(submissionId, latestSubmittedEvent.FileId, cancellationToken);

                response.LastSubmittedFile = new SubmittedPomFileInformation
                {
                    SubmittedDateTime = latestSubmittedEvent.Created,
                    SubmittedBy = latestSubmittedEvent.UserId,
                    FileName = submittedFileAntivirusCheckEvent.FileName,
                    FileId = submittedFileAntivirusCheckEvent.FileId
                };
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
            .FirstAsync(cancellationToken);

        return await _submissionEventQueryRepository
            .GetAll(x => x.Type == EventType.AntivirusCheck)
            .Cast<AntivirusCheckEvent>()
            .Where(x => x.FileId == antivirusResultEvent.FileId)
            .FirstAsync(cancellationToken);
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

    private async Task<List<AbstractValidationEvent>> GetInvalidProducerValidationEventsByBlobNameAsync(
        string blobName,
        CancellationToken cancellationToken)
    {
        return await _submissionEventQueryRepository
            .GetAll(x => (x.Type == EventType.ProducerValidation || x.Type == EventType.CheckSplitter)
                         && x.BlobName == blobName)
            .Cast<AbstractValidationEvent>()
            .Where(x => x.IsValid == false)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> BlobHasWarningsAsync(
        string blobName,
        CancellationToken cancellationToken)
    {
        return await _submissionEventQueryRepository
            .GetAll(x => (x.Type == EventType.ProducerValidation || x.Type == EventType.CheckSplitter)
                && x.BlobName == blobName)
            .Cast<AbstractValidationEvent>()
            .Where(x => x.HasWarnings == true)
            .CountAsync(cancellationToken) > 0;
    }

    private async Task<int> GetProducerValidationEventCountByBlobNameAsync(string blobName, CancellationToken cancellationToken)
    {
        return await _submissionEventQueryRepository
            .GetAll(x => x.Type == EventType.ProducerValidation && x.BlobName == blobName)
            .Cast<ProducerValidationEvent>()
            .CountAsync(cancellationToken);
    }
}