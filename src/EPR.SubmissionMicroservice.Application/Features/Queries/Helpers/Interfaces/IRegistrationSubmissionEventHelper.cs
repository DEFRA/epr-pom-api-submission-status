namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using Common;

public interface IRegistrationSubmissionEventHelper
{
    Task SetValidationEvents(RegistrationSubmissionGetResponse response, bool isSubmitted, CancellationToken cancellationToken);
}