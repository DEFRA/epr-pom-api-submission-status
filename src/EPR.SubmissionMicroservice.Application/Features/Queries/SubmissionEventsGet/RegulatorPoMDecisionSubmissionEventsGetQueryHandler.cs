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
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionEventQueryRepository;
    private readonly IMapper _mapper;

    public RegulatorPoMDecisionSubmissionEventsGetQueryHandler(
        IQueryRepository<RegulatorPoMDecisionEvent> submissionQueryRepository,
        IQueryRepository<AbstractSubmissionEvent> submissionEventQueryRepository,
        IMapper mapper)
    {
        _submissionQueryRepository = submissionQueryRepository;
        _submissionEventQueryRepository = submissionEventQueryRepository;
        _mapper = mapper;
    }

    public async Task<ErrorOr<List<AbstractSubmissionEventGetResponse>>> Handle(
        RegulatorPoMDecisionSubmissionEventsGetQuery request,
        CancellationToken cancellationToken)
    {
        var submissionEvent = await _submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == request.SubmissionId && x.Type == EventType.Submitted)
            .OrderByDescending(x => x.Created)
            .Where(x => x.Created > request.LastSyncTime)
            .Cast<SubmittedEvent>()
            .FirstOrDefaultAsync(cancellationToken);

        var decisionEvent = await _submissionQueryRepository
            .GetAll(x => x.SubmissionId == request.SubmissionId && x.Type == EventType.RegulatorPoMDecision)
            .OrderByDescending(x => x.Created)
            .Where(x => x.Created > request.LastSyncTime)
            .Cast<RegulatorPoMDecisionEvent>()
            .FirstOrDefaultAsync(cancellationToken);

        var submissionsWithEvents = new List<AbstractSubmissionEventGetResponse>();

        if (submissionEvent != null)
        {
            submissionsWithEvents.Add(new RegulatorDecisionGetResponse
            {
                FileId = submissionEvent.FileId,
                Comments = string.Empty,
                Decision = string.Empty,
                IsResubmissionRequired = false,
                Type = SubmissionType.Producer,
                Created = submissionEvent.Created,
                SubmissionId = submissionEvent.SubmissionId
            });
        }

        if (decisionEvent != null)
        {
            var response = _mapper.Map<RegulatorDecisionGetResponse>(decisionEvent);
            submissionsWithEvents.Add(response);
        }

        return submissionsWithEvents;
    }
}