using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;

public class AccreditationSubmissionEventHelper : IAccreditationSubmissionEventHelper
{
    private readonly IQueryRepository<AbstractSubmissionEvent> _accreditationEventQueryRepository;

    public AccreditationSubmissionEventHelper(
        IQueryRepository<AbstractSubmissionEvent> accreditationEventQueryRepository)
    {
        _accreditationEventQueryRepository = accreditationEventQueryRepository;
    }

    public async Task SetValidationEventsAsync(AccreditationSubmissionGetResponse response, bool isSubmitted, CancellationToken cancellationToken)
    {
        var submissionId = response.Id;
        var fileUploadErrors = new List<string>();
        var antivirusCheckEvent = await GetAntivirusCheckEventAsync(submissionId, cancellationToken);

        if (antivirusCheckEvent is null)
        {
            return;
        }

        if (antivirusCheckEvent.Errors.Count > 0)
        {
            fileUploadErrors.AddRange(antivirusCheckEvent.Errors);
        }

        var antivirusResultEvent = await GetAntivirusResultEventByFileIdAsync(antivirusCheckEvent.FileId, cancellationToken);
        var processingComplete = false;

        if (antivirusResultEvent is not null
            && antivirusCheckEvent.Errors.Count == 0
            && antivirusResultEvent.Errors.Count == 0
            && antivirusResultEvent.AntivirusScanResult == AntivirusScanResult.Success)
        {
            processingComplete = true;
        }

        if (antivirusResultEvent is not null && antivirusResultEvent.Errors.Count > 0)
        {
            fileUploadErrors.AddRange(antivirusResultEvent.Errors);
        }

        response.UserId = antivirusCheckEvent.UserId;
        response.FileId = antivirusCheckEvent.FileId;
        response.AccreditationFileName = antivirusCheckEvent.FileName;
        response.AccreditationFileUploadDateTime = antivirusCheckEvent.Created;
        response.ValidationPass = true;
        response.Errors = fileUploadErrors;
        response.HasWarnings = false;
        response.AccreditationDataComplete = processingComplete;
    }

    private async Task<AntivirusCheckEvent?> GetAntivirusCheckEventAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        return await _accreditationEventQueryRepository
            .GetAll(x => x.SubmissionId == submissionId && x.Type == EventType.AntivirusCheck)
            .OrderByDescending(x => x.Created)
            .Cast<AntivirusCheckEvent>()
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AntivirusResultEvent?> GetAntivirusResultEventByFileIdAsync(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        return await _accreditationEventQueryRepository
            .GetAll(x => x.Type == EventType.AntivirusResult)
            .Cast<AntivirusResultEvent>()
            .Where(x => x.FileId == fileId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
