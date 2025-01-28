namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsEventsGet;

using System.Linq;
using Application.Features.Queries.Common;
using Data.Entities.AntivirusEvents;
using Data.Entities.SubmissionEvent;
using Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class SubmissionsEventsGetQueryHandler : IRequestHandler<SubmissionsEventsGetQuery, ErrorOr<SubmissionsEventsGetResponse>>
{
    private readonly IQueryRepository<SubmittedEvent> _submittedEventQueryRepository;
    private readonly IQueryRepository<RegulatorPoMDecisionEvent> _regulatorPoMDecisionEventQueryRepository;
    private readonly IQueryRepository<AntivirusCheckEvent> _antivirusCheckEventQueryRepository;

    public SubmissionsEventsGetQueryHandler(IQueryRepository<SubmittedEvent> submittedEventQueryRepository, IQueryRepository<RegulatorPoMDecisionEvent> regulatorPoMDecisionEventQueryRepository, IQueryRepository<AntivirusCheckEvent> antivirusCheckEventQueryRepository)
    {
        _submittedEventQueryRepository = submittedEventQueryRepository;
        _regulatorPoMDecisionEventQueryRepository = regulatorPoMDecisionEventQueryRepository;
        _antivirusCheckEventQueryRepository = antivirusCheckEventQueryRepository;
    }

    public async Task<ErrorOr<SubmissionsEventsGetResponse>> Handle(SubmissionsEventsGetQuery request, CancellationToken cancellationToken)
    {
        var response = new SubmissionsEventsGetResponse();

        var submittedEvents = await _submittedEventQueryRepository.GetAll(x =>
            x.SubmissionId == request.SubmissionId &&
            x.Created > request.LastSyncTime)
            .OrderByDescending(x => x.Created)
            .ToListAsync(cancellationToken);

        var regulatorPOMDecisionEvents = await _regulatorPoMDecisionEventQueryRepository.GetAll(x =>
            x.SubmissionId == request.SubmissionId &&
            x.Created > request.LastSyncTime)
            .OrderByDescending(x => x.Created)
            .ToListAsync(cancellationToken);

        var antivirusCheckEvents = await _antivirusCheckEventQueryRepository.GetAll(x =>
            x.SubmissionId == request.SubmissionId &&
            x.Created > request.LastSyncTime)
            .OrderByDescending(x => x.Created)
            .ToListAsync(cancellationToken);

        if (submittedEvents.Count > 0)
        {
            response.SubmittedEvents
                .AddRange(submittedEvents
                .Select(x => new SubmittedEvents
                {
                    FileId = x.FileId,
                    Created = x.Created,
                    FileName = GetFileName(antivirusCheckEvents, x.FileId),
                    SubmissionId = x.SubmissionId,
                    SubmittedBy = x.SubmittedBy,
                    UserId = x.UserId
                }));
        }

        if (regulatorPOMDecisionEvents.Count > 0)
        {
            response.RegulatorDecisionEvents
                .AddRange(regulatorPOMDecisionEvents
                .Select(x => new RegulatorDecisionEvents
                {
                    UserId = x.UserId,
                    SubmissionId = x.SubmissionId,
                    Comment = x.Comments,
                    Created = x.Created,
                    Decision = x.Decision.ToString(),
                    FileId = x.FileId,
                    FileName = GetFileName(antivirusCheckEvents, x.FileId),
                }));
        }

        if (antivirusCheckEvents.Count > 0)
        {
            response.AntivirusCheckEvents
                .AddRange(antivirusCheckEvents
                .Select(x => new AntivirusCheckEvents
                {
                    Created = x.Created,
                    FileId = x.FileId,
                    FileName = x.FileName,
                    SubmissionId = x.SubmissionId,
                    UserId = x.UserId
                }));
        }

        return response;
    }

    private static string GetFileName(List<AntivirusCheckEvent> antivirusCheckEvents, Guid fileId)
    {
        return antivirusCheckEvents
                .Where(x => x.FileId == fileId)
                .Select(x => x.FileName)
                .SingleOrDefault();
    }
}