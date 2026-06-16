using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;

public interface IValidationEventHelper
{
    public Task<AntivirusResultEvent?> GetLatestAntivirusResult(Guid submissionId, CancellationToken cancellationToken);

    public Task<AntivirusResultEvent?> GetAntivirusResultByFileId(Guid submissionId, Guid fileId, CancellationToken cancellationToken);
}