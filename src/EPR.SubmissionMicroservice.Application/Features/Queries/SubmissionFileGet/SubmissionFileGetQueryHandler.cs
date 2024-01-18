namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionFileGet;

using Data.Entities.AntivirusEvents;
using Data.Entities.Submission;
using Data.Entities.SubmissionEvent;
using Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;

public class SubmissionFileGetQueryHandler
    : IRequestHandler<SubmissionFileGetQuery, ErrorOr<SubmissionFileGetResponse>>
{
    private readonly IQueryRepository<Submission> _submissionQueryRepository;
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionEventQueryRepository;

    public SubmissionFileGetQueryHandler(
        IQueryRepository<Submission> submissionQueryRepository,
        IQueryRepository<AbstractSubmissionEvent> submissionEventQueryRepository)
    {
        _submissionQueryRepository = submissionQueryRepository;
        _submissionEventQueryRepository = submissionEventQueryRepository;
    }

    public async Task<ErrorOr<SubmissionFileGetResponse>> Handle(
        SubmissionFileGetQuery request,
        CancellationToken cancellationToken)
    {
        var antivirusCheckEvent = _submissionEventQueryRepository
            .GetAll(x => x is AntivirusCheckEvent)
            .OfType<AntivirusCheckEvent>()
            .FirstOrDefault(x => x.FileId == request.FileId);

        if (antivirusCheckEvent == null)
        {
            return Error.NotFound();
        }

        var submission = await _submissionQueryRepository.GetByIdAsync(antivirusCheckEvent.SubmissionId, cancellationToken);

        return new SubmissionFileGetResponse
        {
            SubmissionId = antivirusCheckEvent.SubmissionId,
            SubmissionType = submission.SubmissionType,
            FileId = antivirusCheckEvent.FileId,
            FileName = antivirusCheckEvent.FileName,
            FileType = antivirusCheckEvent.FileType,
            UserId = antivirusCheckEvent.UserId,
            OrganisationId = submission.OrganisationId,
            SubmissionPeriod = submission.SubmissionPeriod,
            ComplianceSchemeId = submission.ComplianceSchemeId,
            Errors = antivirusCheckEvent.Errors?.OrderBy(x => x).ToList()
        };
    }
}