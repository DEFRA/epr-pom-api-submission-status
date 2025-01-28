namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionUploadedFileGet;

using System.Linq;
using Data.Entities.AntivirusEvents;
using Data.Entities.Submission;
using Data.Entities.SubmissionEvent;
using Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;

public class SubmissionUploadedFileGetQueryHandler
    : IRequestHandler<SubmissionUploadedFileGetQuery, ErrorOr<SubmissionUploadedFileGetResponse>>
{
    private readonly IQueryRepository<Submission> _submissionQueryRepository;
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionEventQueryRepository;

    public SubmissionUploadedFileGetQueryHandler(
        IQueryRepository<Submission> submissionQueryRepository,
        IQueryRepository<AbstractSubmissionEvent> submissionEventQueryRepository)
    {
        _submissionQueryRepository = submissionQueryRepository;
        _submissionEventQueryRepository = submissionEventQueryRepository;
    }

    public async Task<ErrorOr<SubmissionUploadedFileGetResponse>> Handle(
        SubmissionUploadedFileGetQuery request,
        CancellationToken cancellationToken)
    {
        var antivirusResultEvent = _submissionEventQueryRepository
            .GetAll(x => x is AntivirusResultEvent)
            .OfType<AntivirusResultEvent>()
            .Where(x => x.SubmissionId == request.SubmissionId && x.FileId == request.FileId)
            .OrderByDescending(x => x.Created)
            .FirstOrDefault();

        if (antivirusResultEvent == null)
        {
            return Error.NotFound();
        }

        var submission = await _submissionQueryRepository.GetByIdAsync(antivirusResultEvent.SubmissionId, cancellationToken);

        return new SubmissionUploadedFileGetResponse
        {
            SubmissionId = antivirusResultEvent.SubmissionId,
            SubmissionType = submission.SubmissionType,
            FileId = antivirusResultEvent.FileId,
            UserId = antivirusResultEvent.UserId,
            OrganisationId = submission.OrganisationId,
            BlobName = antivirusResultEvent.BlobName,
            AntivirusScanResult = antivirusResultEvent.AntivirusScanResult,
            AntivirusScanTrigger = antivirusResultEvent.AntivirusScanTrigger,
            Errors = antivirusResultEvent.Errors?.OrderBy(x => x).ToList()
        };
    }
}