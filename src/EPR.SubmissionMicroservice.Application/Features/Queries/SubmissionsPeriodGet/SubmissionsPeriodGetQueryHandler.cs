namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsPeriodGet;

using AutoMapper;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class SubmissionsPeriodGetQueryHandler : IRequestHandler<SubmissionsPeriodGetQuery, ErrorOr<List<SubmissionGetResponse>>>
{
    private readonly IQueryRepository<Submission> _submissionQueryRepository;
    private readonly IMapper _mapper;

    public SubmissionsPeriodGetQueryHandler(IQueryRepository<Submission> submissionQueryRepository, IMapper mapper)
    {
        _submissionQueryRepository = submissionQueryRepository;
        _mapper = mapper;
    }

    public async Task<ErrorOr<List<SubmissionGetResponse>>> Handle(SubmissionsPeriodGetQuery request, CancellationToken cancellationToken)
    {
        if (request.Year is not null && request.Year > DateTime.Now.Year)
        {
            return Error.Failure("Provided year is greater then current year");
        }

        var query = _submissionQueryRepository.GetAll(
            x => x.OrganisationId == request.OrganisationId
                && x.SubmissionType == request.Type
                && x.Created != null);

        if (request.ComplianceSchemeId is not null)
        {
            query = query.Where(x => x.ComplianceSchemeId == request.ComplianceSchemeId);
        }

        if (request.Year is not null)
        {
            var yearStart = new DateTime((int)request.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextYearStart = yearStart.AddYears(1);

            query = query.Where(x => yearStart <= x.Created && x.Created < nextYearStart);
        }

        var result = await query.OrderByDescending(x => x.Created).ToListAsync(cancellationToken);

        return _mapper.Map<List<SubmissionGetResponse>>(result);
    }
}