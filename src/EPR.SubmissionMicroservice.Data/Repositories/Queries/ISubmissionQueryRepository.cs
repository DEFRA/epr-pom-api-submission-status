using EPR.SubmissionMicroservice.Data.Entities.Submission;

namespace EPR.SubmissionMicroservice.Data.Repositories.Queries;

public interface ISubmissionQueryRepository
{
    Task<Submission?> GetSubmissionAsync(Guid submissionId, CancellationToken cancellationToken);
}
