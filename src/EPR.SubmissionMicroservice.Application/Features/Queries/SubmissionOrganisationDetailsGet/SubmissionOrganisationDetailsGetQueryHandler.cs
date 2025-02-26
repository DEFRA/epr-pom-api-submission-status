namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionOrganisationDetailsGet;

using Data.Entities.SubmissionEvent;
using Data.Repositories.Queries.Interfaces;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Enums;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class SubmissionOrganisationDetailsGetQueryHandler
    : IRequestHandler<SubmissionOrganisationDetailsGetQuery, ErrorOr<SubmissionOrganisationDetailsGetResponse>>
{
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionEventsQueryRepository;
    private readonly ILogger<SubmissionOrganisationDetailsGetQueryHandler> _logger;

    public SubmissionOrganisationDetailsGetQueryHandler(
        IQueryRepository<AbstractSubmissionEvent> submissionEventsQueryRepository,
        ILogger<SubmissionOrganisationDetailsGetQueryHandler> logger)
    {
        _submissionEventsQueryRepository = submissionEventsQueryRepository;
        _logger = logger;
    }

    public async Task<ErrorOr<SubmissionOrganisationDetailsGetResponse>> Handle(
        SubmissionOrganisationDetailsGetQuery request,
        CancellationToken cancellationToken)
    {
        var requiredEventTypes = new List<EventType>
        {
            EventType.AntivirusCheck,
            EventType.AntivirusResult,
            EventType.Registration,
            EventType.BrandValidation,
            EventType.PartnerValidation,
        };

        var submissionEvents = await _submissionEventsQueryRepository
            .GetAll(x => x.SubmissionId == request.SubmissionId &&
                         requiredEventTypes.Contains(x.Type))
            .OrderByDescending(x => x.Created)
            .ToListAsync(cancellationToken);

        var antiVirusResultEvent = submissionEvents
            .OfType<AntivirusResultEvent>()
            .FirstOrDefault(x => x.BlobName == request.BlobName);

        if (antiVirusResultEvent is null)
        {
            _logger.LogInformation("AntiVirusResultEvent not found for blob {BlobName} in submission {SubmissionId}", request.BlobName, request.SubmissionId);
            return Error.NotFound();
        }

        var antiVirusCheckEvent = submissionEvents
            .OfType<AntivirusCheckEvent>()
            .FirstOrDefault(x => x.FileId == antiVirusResultEvent.FileId);

        if (antiVirusCheckEvent is null)
        {
            _logger.LogInformation("AntiVirusCheckEvent not found for blob {BlobName} with fileId {FileId} in submission {SubmissionId}", request.BlobName, antiVirusResultEvent.FileId, request.SubmissionId);
            return Error.NotFound();
        }

        var registrationAntiVirusCheckEvent = submissionEvents
            .OfType<AntivirusCheckEvent>()
            .FirstOrDefault(x => x.RegistrationSetId == antiVirusCheckEvent.RegistrationSetId &&
                                 x.FileType == FileType.CompanyDetails);

        if (registrationAntiVirusCheckEvent is null)
        {
            _logger.LogInformation("RegistrationAntiVirusCheckEvent not found for blob {BlobName} with registrationSetId {RegistrationSetId} in submission {SubmissionId}", request.BlobName, antiVirusCheckEvent.RegistrationSetId, request.SubmissionId);
            return Error.NotFound();
        }

        var registrationAntiVirusResultEvent = submissionEvents
            .OfType<AntivirusResultEvent>()
            .FirstOrDefault(x => x.FileId == registrationAntiVirusCheckEvent.FileId);

        if (registrationAntiVirusResultEvent is null ||
            registrationAntiVirusResultEvent.BlobName is null)
        {
            _logger.LogInformation("RegistrationAntiVirusResultEvent not found for blob {BlobName} with fileId {FileId} in submission {SubmissionId}", request.BlobName, registrationAntiVirusCheckEvent.FileId, request.SubmissionId);
            return Error.NotFound();
        }

        _logger.LogInformation("Found RegistrationAntiVirusResultEvent with blob name {BlobName} in submission {SubmissionId}", registrationAntiVirusResultEvent.BlobName, request.SubmissionId);

        return new SubmissionOrganisationDetailsGetResponse
        {
            BlobName = registrationAntiVirusResultEvent.BlobName,
        };
    }
}