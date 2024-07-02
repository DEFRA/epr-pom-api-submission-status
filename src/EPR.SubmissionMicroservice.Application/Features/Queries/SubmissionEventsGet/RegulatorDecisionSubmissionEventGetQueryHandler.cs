using AutoMapper;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorDecisionSubmissionEventGetQueryHandler : IRequestHandler<RegulatorDecisionSubmissionEventGetQuery, ErrorOr<List<AbstractSubmissionEventGetResponse>>>
{
    private readonly IQueryRepository<RegulatorPoMDecisionEvent> _regulatorPoMDecisionEventQueryRepository;
    private readonly IQueryRepository<RegulatorRegistrationDecisionEvent> _regulatorRegistrationDecisionEventQueryRepository;
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionEventQueryRepository;
    private readonly IMapper _mapper;

    public RegulatorDecisionSubmissionEventGetQueryHandler(
            IQueryRepository<RegulatorPoMDecisionEvent> regulatorPoMDecisionEventQueryRepository,
            IQueryRepository<RegulatorRegistrationDecisionEvent> regulatorRegistrationDecisionEventQueryRepository,
            IQueryRepository<AbstractSubmissionEvent> submissionEventQueryRepository,
            IMapper mapper)
    {
        _regulatorPoMDecisionEventQueryRepository = regulatorPoMDecisionEventQueryRepository;
        _regulatorRegistrationDecisionEventQueryRepository = regulatorRegistrationDecisionEventQueryRepository;
        _submissionEventQueryRepository = submissionEventQueryRepository;
        _mapper = mapper;
    }

    public async Task<ErrorOr<List<AbstractSubmissionEventGetResponse>>> Handle(
    RegulatorDecisionSubmissionEventGetQuery request,
    CancellationToken cancellationToken)
    {
        var submissionEvent = await _submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == request.SubmissionId && x.Type == EventType.Submitted)
            .OrderByDescending(x => x.Created)
            .Where(x => x.Created > request.LastSyncTime)
            .Cast<SubmittedEvent>()
            .FirstOrDefaultAsync(cancellationToken);

        var incomingEventType = request.Type switch
        {
            SubmissionType.Producer => EventType.RegulatorPoMDecision,
            SubmissionType.Registration => EventType.RegulatorRegistrationDecision,
            _ => EventType.RegulatorPoMDecision
        };

        AbstractSubmissionEvent decisionEvent = null;

        if (incomingEventType == EventType.RegulatorRegistrationDecision)
        {
            decisionEvent = await _regulatorRegistrationDecisionEventQueryRepository
                .GetAll(x => x.SubmissionId == request.SubmissionId && x.Type == incomingEventType)
                .OrderByDescending(x => x.Created)
                .Where(x => x.Created > request.LastSyncTime)
                .Cast<RegulatorRegistrationDecisionEvent>()
                .FirstOrDefaultAsync(cancellationToken);
        }
        else if (incomingEventType == EventType.RegulatorPoMDecision)
        {
            decisionEvent = await _regulatorPoMDecisionEventQueryRepository
                .GetAll(x => x.SubmissionId == request.SubmissionId && x.Type == incomingEventType)
                .OrderByDescending(x => x.Created)
                .Where(x => x.Created > request.LastSyncTime)
                .Cast<RegulatorPoMDecisionEvent>()
                .FirstOrDefaultAsync(cancellationToken);
        }

        var submissionsWithEvents = new List<AbstractSubmissionEventGetResponse>();

        if (submissionEvent != null)
        {
            submissionsWithEvents.Add(new RegulatorDecisionGetResponse
            {
                FileId = submissionEvent.FileId,
                Comments = string.Empty,
                Decision = string.Empty,
                IsResubmissionRequired = false,
                Type = request.Type,
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