using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common.Interfaces;
using ErrorOr;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionGet;

/// <summary>
/// Fills in extra data on the submission that is missing from the database (perhaps due to data issues)
/// </summary>
internal class HydrateSubmissionBehaviour : IPipelineBehavior<SubmissionGetQuery, ErrorOr<AbstractSubmissionGetResponse>>
{
    private readonly ISubmissionHydrationService _submissionHydrationService;

    public HydrateSubmissionBehaviour(ISubmissionHydrationService submissionHydrationService)
    {
        _submissionHydrationService = submissionHydrationService;
    }

    public async Task<ErrorOr<AbstractSubmissionGetResponse>> Handle(SubmissionGetQuery request, RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (response.IsError)
        {
            return response;
        }
        
        _submissionHydrationService.Hydrate(response.Value);
        
        return response;
    }
}