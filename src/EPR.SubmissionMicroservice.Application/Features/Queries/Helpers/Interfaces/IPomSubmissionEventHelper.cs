namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using Common;

public interface IPomSubmissionEventHelper
{
    Task SetValidationEventsAsync(PomSubmissionGetResponse response, bool isSubmitted, CancellationToken cancellationToken);

    Task<bool> VerifyFileIdIsForValidFileAsync(Guid submissionId, Guid fileId, CancellationToken cancellationToken);
}