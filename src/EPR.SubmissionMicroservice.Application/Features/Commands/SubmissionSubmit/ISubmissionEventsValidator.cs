namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;

public interface ISubmissionEventsValidator
{
    Task<bool> IsSubmissionValidAsync(Guid submissionId, Guid fileId, CancellationToken cancellationToken);
}