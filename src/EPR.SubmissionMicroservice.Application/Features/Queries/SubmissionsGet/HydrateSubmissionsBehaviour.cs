namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionsGet;

using Common;
using Common.Interfaces;
using ErrorOr;
using MediatR;

/// <summary>
/// Fills in extra data on each submission in the list that is missing from the database (perhaps due to data issues)
/// </summary>
internal class HydrateSubmissionsBehaviour : IPipelineBehavior<SubmissionsGetQuery, ErrorOr<List<AbstractSubmissionGetResponse>>>
{
    private readonly ISubmissionHydrationService _submissionHydrationService;

    public HydrateSubmissionsBehaviour(ISubmissionHydrationService submissionHydrationService)
    {
        _submissionHydrationService = submissionHydrationService;
    }

    public async Task<ErrorOr<List<AbstractSubmissionGetResponse>>> Handle(
        SubmissionsGetQuery request,
        RequestHandlerDelegate<ErrorOr<List<AbstractSubmissionGetResponse>>> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (response.IsError)
        {
            return response;
        }

        foreach (var submission in response.Value)
        {
            _submissionHydrationService.Hydrate(submission);
        }

        return response;
    }
}
