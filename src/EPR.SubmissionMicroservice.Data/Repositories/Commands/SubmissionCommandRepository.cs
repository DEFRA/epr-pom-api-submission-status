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
    
    public async Task AddSubmitEventAsync(SubmittedEvent submittedEvent, CancellationToken cancellationToken)
    {
        await submissionEventCommandRepository.AddAsync(submittedEvent);
    }
    
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await submissionContext.SaveChangesAsync(cancellationToken);
    }
}
