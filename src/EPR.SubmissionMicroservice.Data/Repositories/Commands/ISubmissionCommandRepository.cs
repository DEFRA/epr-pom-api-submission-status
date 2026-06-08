using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

namespace EPR.SubmissionMicroservice.Data.Repositories.Commands;

public interface ISubmissionCommandRepository
{
    void UpdateSubmission(Submission submission);
    
    Task AddSubmitEventAsync(SubmittedEvent submittedEvent, CancellationToken cancellationToken);
    
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
