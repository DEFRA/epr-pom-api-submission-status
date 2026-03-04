using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Enums;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionGet;

/// <summary>
/// Fills in extra data on the submission that is missing from the database (perhaps due to data issues)
/// </summary>
internal class HydrateSubmissionBehaviour : IPipelineBehavior<SubmissionGetQuery, ErrorOr<AbstractSubmissionGetResponse>>
{
    public async Task<ErrorOr<AbstractSubmissionGetResponse>> Handle(SubmissionGetQuery request, RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (response.IsError)
        {
            return response;
        }
        
        var submission = response.Value;
        
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
        
        return submission;
    }
}