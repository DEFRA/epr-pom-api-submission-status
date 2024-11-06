using AutoMapper;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

public class RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryHandler : IRequestHandler<RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery, ErrorOr<List<RegulatorOrganisationRegistrationDecisionGetResponse>>>
{
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionQueryRepository;
    private readonly IMapper _mapper;

    public RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryHandler(
        IQueryRepository<AbstractSubmissionEvent> submissionQueryRepository,
        IMapper mapper)
    {
        _submissionQueryRepository = submissionQueryRepository;
        _mapper = mapper;
    }

    public async Task<ErrorOr<List<RegulatorOrganisationRegistrationDecisionGetResponse>>> Handle(
        RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery query,
        CancellationToken cancellationToken)
    {
        var submissionEventsQuery = _submissionQueryRepository
                                    .GetAll(x => x.Type == EventType.RegulatorOrganisationRegistrationDecision)
                                    .Where(x => x.Created > query.LastSyncTime);

        if (query.SubmissionId != null)
        {
            submissionEventsQuery = submissionEventsQuery.Where(x => x.SubmissionId == query.SubmissionId);
        }

        var submissionEvents = await submissionEventsQuery.ToListAsync(cancellationToken);

        return submissionEvents
               .Select(submission => _mapper.Map<RegulatorOrganisationRegistrationDecisionGetResponse>(submission))
               .ToList();
    }
}
