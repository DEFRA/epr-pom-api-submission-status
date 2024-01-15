using AutoMapper;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorPoMDecisionSubmissionEventsGetQueryHandler : IRequestHandler<RegulatorPoMDecisionSubmissionEventsGetQuery, ErrorOr<List<AbstractSubmissionEventGetResponse>>>
{
    private readonly IQueryRepository<RegulatorPoMDecisionEvent> _submissionQueryRepository;
    private readonly IMapper _mapper;

    public RegulatorPoMDecisionSubmissionEventsGetQueryHandler(
        IQueryRepository<RegulatorPoMDecisionEvent> submissionQueryRepository,
        IMapper mapper)
    {
        _submissionQueryRepository = submissionQueryRepository;
        _mapper = mapper;
    }

    public async Task<ErrorOr<List<AbstractSubmissionEventGetResponse>>> Handle(
        RegulatorPoMDecisionSubmissionEventsGetQuery request,
        CancellationToken cancellationToken)
    {
        var submissionEvents = await _submissionQueryRepository
            .GetAll(x => x.Type == EventType.RegulatorPoMDecision)
            .OrderByDescending(x => x.Created)
            .Where(x => x.Created > request.LastSyncTime)
            .ToListAsync(cancellationToken);

        var submissionsWithEvents = new List<AbstractSubmissionEventGetResponse>();

        foreach (var submission in submissionEvents)
        {
            var pomResponse = _mapper.Map<RegulatorDecisionGetResponse>(submission);
            submissionsWithEvents.Add(pomResponse);
        }

        return submissionsWithEvents;
    }
}