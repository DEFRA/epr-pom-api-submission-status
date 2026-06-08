using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Repositories.Commands.Interfaces;

namespace EPR.SubmissionMicroservice.Data.Repositories.Commands;

public class SubmissionCommandRepository(
    ICommandRepository<Submission> submissionCommandRepository,
    ICommandRepository<AbstractSubmissionEvent> submissionEventCommandRepository,
    SubmissionContext submissionContext)
    : ISubmissionCommandRepository
{
    public void UpdateSubmission(Submission submission)
    {
        submissionCommandRepository.Update(submission);
    }
    
    public Task AddSubmitEventAsync(SubmittedEvent submittedEvent)
    {
        return submissionEventCommandRepository.AddAsync(submittedEvent);
    }
    
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return submissionContext.SaveChangesAsync(cancellationToken);
    }
}
