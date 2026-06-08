using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Data.Repositories.Queries;

public class SubmissionQueryRepository(
    IQueryRepository<Submission> submissionQueryRepository)
    : ISubmissionQueryRepository
{
    public async Task<Submission?> GetSubmissionAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        return await submissionQueryRepository.GetByIdAsync(submissionId, cancellationToken);
    }
}
