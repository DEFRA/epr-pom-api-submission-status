namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using Common;

public interface IRegistrationSubmissionEventHelper
{
    Task SetValidationEvents(RegistrationSubmissionGetResponse response, bool isSubmitted, CancellationToken cancellationToken);

    Task<bool> VerifyFileIdIsForValidFileAsync(Guid submissionId, Guid fileId, CancellationToken cancellationToken);
}