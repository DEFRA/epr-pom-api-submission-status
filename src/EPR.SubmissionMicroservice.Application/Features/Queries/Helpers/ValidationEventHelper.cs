using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;

public class ValidationEventHelper : IValidationEventHelper
{
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionEventQueryRepository;

    public ValidationEventHelper(IQueryRepository<AbstractSubmissionEvent> submissionEventQueryRepository)
    {
        _submissionEventQueryRepository = submissionEventQueryRepository;
    }

    public async Task<AntivirusResultEvent?> GetLatestAntivirusResult(Guid submissionId, CancellationToken cancellationToken)
    {
        var latestAntivirusCheckEvent = await _submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submissionId && x.Type == EventType.AntivirusCheck)
            .OrderByDescending(x => x.Created)
            .Cast<AntivirusCheckEvent>()
            .FirstOrDefaultAsync(cancellationToken);

        if (latestAntivirusCheckEvent == null)
        {
            return null;
        }

        var latestAntivirusResultEvent = await _submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submissionId && x.Type == EventType.AntivirusResult)
            .OrderByDescending(x => x.Created)
            .Cast<AntivirusResultEvent>()
            .Where(x => x.FileId == latestAntivirusCheckEvent.FileId)
            .FirstOrDefaultAsync(cancellationToken);

        return latestAntivirusResultEvent;
    }
}