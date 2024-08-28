namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsGet;

using AutoMapper;
using Common;
using Data.Entities.Submission;
using Data.Enums;
using Data.Repositories.Queries.Interfaces;
using ErrorOr;
using Helpers.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class SubmissionsGetQueryHandler : IRequestHandler<SubmissionsGetQuery, ErrorOr<List<AbstractSubmissionGetResponse>>>
{
    private readonly IQueryRepository<Submission> _submissionQueryRepository;
    private readonly IPomSubmissionEventHelper _pomSubmissionEventHelper;
    private readonly IRegistrationSubmissionEventHelper _registrationSubmissionEventHelper;
    private readonly IMapper _mapper;

    public SubmissionsGetQueryHandler(
        IQueryRepository<Submission> submissionQueryRepository,
        IPomSubmissionEventHelper pomSubmissionEventHelper,
        IRegistrationSubmissionEventHelper registrationSubmissionEventHelper,
        IMapper mapper)
    {
        _submissionQueryRepository = submissionQueryRepository;
        _pomSubmissionEventHelper = pomSubmissionEventHelper;
        _registrationSubmissionEventHelper = registrationSubmissionEventHelper;
        _mapper = mapper;
    }

    public async Task<ErrorOr<List<AbstractSubmissionGetResponse>>> Handle(SubmissionsGetQuery request, CancellationToken cancellationToken)
    {
        var submissions = await GetSubmissionsAsync(request, cancellationToken);

        var submissionsWithEvents = new List<AbstractSubmissionGetResponse>();

        foreach (var submission in submissions)
        {
            switch (submission.SubmissionType)
            {
                case SubmissionType.Producer:
                    var pomResponse = _mapper.Map<PomSubmissionGetResponse>(submission);
                    await _pomSubmissionEventHelper.SetValidationEventsAsync(pomResponse, submission is { IsSubmitted: true }, cancellationToken);
                    submissionsWithEvents.Add(pomResponse);
                    break;
                case SubmissionType.Registration:
                    var registrationResponse = _mapper.Map<RegistrationSubmissionGetResponse>(submission);
                    await _registrationSubmissionEventHelper.SetValidationEvents(registrationResponse, submission is { IsSubmitted: true }, cancellationToken);
                    submissionsWithEvents.Add(registrationResponse);
                    break;
                default:
                    throw new ArgumentException("Unknown submissionType");
            }
        }

        return submissionsWithEvents;
    }

    private async Task<List<Submission>> GetSubmissionsAsync(
        SubmissionsGetQuery request,
        CancellationToken cancellationToken)
    {
        var query = _submissionQueryRepository
            .GetAll(x => x.OrganisationId == request.OrganisationId)
            .OrderByDescending(x => x.Created)
            .AsQueryable();

        if (request.Periods is not null && request.Periods.Count > 0)
        {
            query = query.Where(x => request.Periods.Contains(x.SubmissionPeriod));
        }

        if (request.ComplianceSchemeId is not null)
        {
            query = query.Where(x => x.ComplianceSchemeId == request.ComplianceSchemeId);
        }

        if (request.Type is not null)
        {
            query = query.Where(x => x.SubmissionType == request.Type);
        }

        if (request.Limit is > 0)
        {
            query = query.Take(request.Limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }
}