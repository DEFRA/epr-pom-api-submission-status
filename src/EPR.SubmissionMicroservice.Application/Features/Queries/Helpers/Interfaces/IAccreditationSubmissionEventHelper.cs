using EPR.SubmissionMicroservice.Application.Features.Queries.Common;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;

public interface IAccreditationSubmissionEventHelper
{
    Task SetValidationEventsAsync(AccreditationSubmissionGetResponse response, bool isSubmitted, CancellationToken cancellationToken);
}
