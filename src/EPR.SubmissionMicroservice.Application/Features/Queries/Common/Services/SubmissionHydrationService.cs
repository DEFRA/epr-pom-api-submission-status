namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common.Services;

using Data.Enums;
using Interfaces;

/// <summary>
/// Service for hydrating submission responses with missing data
/// </summary>
internal class SubmissionHydrationService : ISubmissionHydrationService
{
    /// <summary>
    /// Hydrates an AbstractSubmissionGetResponse with missing data such as RegistrationYear and RegistrationJourney
    /// </summary>
    /// <param name="submission">The submission response to hydrate</param>
    public void Hydrate(AbstractSubmissionGetResponse submission)
    {
        // fill RegistrationYear and RegistrationJourney properties
        if (submission.SubmissionType == SubmissionType.Registration && submission.SubmissionPeriod.Length >= 4)
        {
            // fill registration year
            // take last 4 characters of submission period
            var registrationYear = submission.SubmissionPeriod[^4..];
            var validYear = int.TryParse(registrationYear, out var parsedYear);

            if (validYear)
            {
                submission.RegistrationYear = parsedYear;

                // fill reg journey for 2026 large producer CSO journeys (that field may be missing because registration closed before RegistrationJourney was introduced)
                if (submission.ComplianceSchemeId is not null && parsedYear == 2026 && submission.RegistrationJourney is null)
                {
                    submission.RegistrationJourney = RegistrationJourney.CsoLargeProducer.ToString();
                }
            }
        }
    }
}
