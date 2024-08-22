namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using Common;

public interface ISubsidiarySubmissionEventHelper
{
    Task SetValidationEventsAsync(SubsidiarySubmissionGetResponse response, bool isSubmitted, CancellationToken cancellationToken);
}