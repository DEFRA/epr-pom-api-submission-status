namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionOrganisationDetailsGet;

using Data.Entities.SubmissionEvent;
using Data.Repositories.Queries.Interfaces;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Enums;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class SubmissionOrganisationDetailsGetQueryHandler
    : IRequestHandler<SubmissionOrganisationDetailsGetQuery, ErrorOr<SubmissionOrganisationDetailsGetResponse>>
{
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionEventsQueryRepository;

    public SubmissionOrganisationDetailsGetQueryHandler(
        IQueryRepository<AbstractSubmissionEvent> submissionEventsQueryRepository)
    {
        _submissionEventsQueryRepository = submissionEventsQueryRepository;
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
            return Error.NotFound();
        }

        var antiVirusCheckEvent = submissionEvents
            .OfType<AntivirusCheckEvent>()
            .FirstOrDefault(x => x.FileId == antiVirusResultEvent.FileId);

        if (antiVirusCheckEvent is null)
        {
            return Error.NotFound();
        }

        var registrationAntiVirusCheckEvent = submissionEvents
            .OfType<AntivirusCheckEvent>()
            .FirstOrDefault(x => x.RegistrationSetId == antiVirusCheckEvent.RegistrationSetId &&
                                 x.FileType == FileType.CompanyDetails);

        if (registrationAntiVirusCheckEvent is null)
        {
            return Error.NotFound();
        }

        var registrationAntiVirusResultEvent = submissionEvents
            .OfType<AntivirusResultEvent>()
            .FirstOrDefault(x => x.FileId == registrationAntiVirusCheckEvent.FileId);

        if (registrationAntiVirusResultEvent is null ||
            registrationAntiVirusResultEvent.BlobName is null)
        {
            return Error.NotFound();
        }

        return new SubmissionOrganisationDetailsGetResponse
        {
            BlobName = registrationAntiVirusResultEvent.BlobName,
        };
    }
}