namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;

using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using Microsoft.EntityFrameworkCore;

public class SubmissionEventsValidator : ISubmissionEventsValidator
{
    private readonly IQueryRepository<AbstractSubmissionEvent> _eventsRepository;

    public SubmissionEventsValidator(IQueryRepository<AbstractSubmissionEvent> eventsRepository)
    {
        _eventsRepository = eventsRepository;
    }

    public static bool IsPartnersFileValid(List<AbstractSubmissionEvent>? submissionEvents, Guid? registrationSetId)
    {
        var partnersAntiVirusCheck = submissionEvents
            .OfType<AntivirusCheckEvent>()
            .Where(x => x.FileType == FileType.Partnerships && x.RegistrationSetId == registrationSetId)
            .MaxBy(x => x.Created);

        var partnersAntiVirusResult = submissionEvents
            .OfType<AntivirusResultEvent>()
            .Where(x => x.FileId == partnersAntiVirusCheck?.FileId)
            .MaxBy(x => x.Created);

        if (partnersAntiVirusResult is not { AntivirusScanResult: AntivirusScanResult.Success })
        {
            return false;
        }

        if (partnersAntiVirusResult.RequiresRowValidation == false)
        {
            return true;
        }

        var partnersValidationEvent = submissionEvents
            .OfType<PartnerValidationEvent>()
            .FirstOrDefault(x => x.BlobName == partnersAntiVirusResult.BlobName);

        return partnersValidationEvent is { IsValid: true };
    }

    public static bool IsBrandsFileValid(List<AbstractSubmissionEvent>? submissionEvents, Guid? registrationSetId)
    {
        var brandsAntiVirusCheck = submissionEvents
            .OfType<AntivirusCheckEvent>()
            .Where(x => x.FileType == FileType.Brands && x.RegistrationSetId == registrationSetId)
            .MaxBy(x => x.Created);

        var brandsAntiVirusResult = submissionEvents
            .OfType<AntivirusResultEvent>()
            .Where(x => x.FileId == brandsAntiVirusCheck?.FileId)
            .MaxBy(x => x.Created);

        if (brandsAntiVirusResult is not { AntivirusScanResult: AntivirusScanResult.Success })
        {
            return false;
        }

        if (brandsAntiVirusResult.RequiresRowValidation == false)
        {
            return true;
        }

        var brandsValidationEvent = submissionEvents
            .OfType<BrandValidationEvent>()
            .FirstOrDefault(x => x.BlobName == brandsAntiVirusResult.BlobName);

        return brandsValidationEvent is { IsValid: true };
    }

    public async Task<bool> IsSubmissionValidAsync(Guid submissionId, Guid fileId, CancellationToken cancellationToken)
    {
        var events = await GetSubmissionEventsAsync(submissionId, cancellationToken);

        var antivirusCheckEvent = events
            .OfType<AntivirusCheckEvent>()
            .FirstOrDefault(x => x.FileId == fileId);
        var antivirusResultEvent = events
            .OfType<AntivirusResultEvent>()
            .FirstOrDefault(x => x.FileId == fileId);

        var registrationSetId = antivirusCheckEvent?.RegistrationSetId;

        if (antivirusResultEvent is not { AntivirusScanResult: AntivirusScanResult.Success })
        {
            return false;
        }

        var registrationEvent = events
            .OfType<RegistrationValidationEvent>()
            .FirstOrDefault(x => x.BlobName == antivirusResultEvent.BlobName);

        if (registrationEvent is null || registrationEvent.IsValid == false)
        {
            return false;
        }

        if (registrationEvent is { RequiresBrandsFile: true } && !IsBrandsFileValid(events, registrationSetId))
        {
            return false;
        }

        if (registrationEvent is { RequiresPartnershipsFile: true } && !IsPartnersFileValid(events, registrationSetId))
        {
            return false;
        }

        return true;
    }

    private async Task<List<AbstractSubmissionEvent>> GetSubmissionEventsAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        return await _eventsRepository
            .GetAll(x => x.SubmissionId == submissionId)
            .ToListAsync(cancellationToken);
    }
}