using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.Submission;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;

public interface ISubmissionFileValidator
{
    Task<bool> IsFileIdForValidFileAsync(Submission submission, Guid fileId, CancellationToken cancellationToken);

    Task<AntivirusResultEvent?> GetAntivirusResultByFileIdAsync(Guid submissionId, Guid fileId, CancellationToken cancellationToken);
}
