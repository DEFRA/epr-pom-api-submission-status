namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common.Interfaces;

/// <summary>
/// Service for hydrating submission responses with missing data
/// </summary>
public interface ISubmissionHydrationService
{
    /// <summary>
    /// Hydrates an AbstractSubmissionGetResponse with missing data such as RegistrationYear and RegistrationJourney
    /// </summary>
    /// <param name="submission">The submission response to hydrate</param>
    void Hydrate(AbstractSubmissionGetResponse submission);
}
