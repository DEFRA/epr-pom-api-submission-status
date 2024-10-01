namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using Common;

public interface ICompaniesHouseSubmissionEventHelper
{
    Task SetValidationEventsAsync(CompaniesHouseSubmissionGetResponse response, CancellationToken cancellationToken);
}