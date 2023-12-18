using EPR.SubmissionMicroservice.API.Contracts.SubmissionEvents.Get;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;

namespace EPR.SubmissionMicroservice.API.Mapping;

using Application.Features.Commands.SubmissionCreate;
using Application.Features.Commands.SubmissionSubmit;
using Application.Features.Queries.SubmissionsGet;
using AutoMapper;
using Contracts.Submission.Create;
using Contracts.Submission.Submit;
using Contracts.Submissions.Get;
using Resolvers;

public class SubmissionProfile : Profile
{
    public SubmissionProfile()
    {
        CreateMap<SubmissionCreateRequest, SubmissionCreateCommand>();

        CreateMap<SubmissionPayload, SubmissionSubmitCommand>();

        CreateMap<RegulatorPoMDecisionSubmissionEventsGetRequest, RegulatorPoMDecisionSubmissionEventsGetQuery>();

        CreateMap<SubmissionsGetRequest, SubmissionsGetQuery>()
            .ForMember(dest => dest.Periods, options =>
                options.MapFrom<SplitStringCommaResolver, string>(src => src.Periods));
    }
}