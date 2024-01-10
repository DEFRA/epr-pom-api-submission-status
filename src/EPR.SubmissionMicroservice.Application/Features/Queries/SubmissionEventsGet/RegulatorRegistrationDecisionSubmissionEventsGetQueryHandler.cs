using AutoMapper;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorRegistrationDecisionSubmissionEventsGetQueryHandler : IRequestHandler<RegulatorRegistrationDecisionSubmissionEventsGetQuery, ErrorOr<List<RegulatorRegistrationDecisionGetResponse>>>
{
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionQueryRepository;
    private readonly IMapper _mapper;

    public RegulatorRegistrationDecisionSubmissionEventsGetQueryHandler(
        IQueryRepository<AbstractSubmissionEvent> submissionQueryRepository,
        IMapper mapper)
    {
        _submissionQueryRepository = submissionQueryRepository;
        _mapper = mapper;
    }

    public async Task<ErrorOr<List<RegulatorRegistrationDecisionGetResponse>>> Handle(
        RegulatorRegistrationDecisionSubmissionEventsGetQuery query,
        CancellationToken cancellationToken)
    {
        var submissionEvents = await _submissionQueryRepository
            .GetAll(x => x.Type == EventType.RegulatorRegistrationDecision)
            .Where(x => x.Created > query.LastSyncTime)
            .ToListAsync(cancellationToken);

        var submissionsDecisionsEvents = new List<RegulatorRegistrationDecisionGetResponse>();

        foreach (var submission in submissionEvents)
        {
            var decisionResponse = _mapper.Map<RegulatorRegistrationDecisionGetResponse>(submission);
            submissionsDecisionsEvents.Add(decisionResponse);
        }

        return submissionsDecisionsEvents;
    }
}