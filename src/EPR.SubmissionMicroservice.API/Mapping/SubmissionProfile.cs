using EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;

namespace EPR.SubmissionMicroservice.API.Mapping;

using API.Contracts.SubmissionEvents.Get;
using Application.Features.Commands.SubmissionCreate;
using Application.Features.Commands.SubmissionSubmit;
using Application.Features.Queries.SubmissionEventsGet;
using Application.Features.Queries.SubmissionsGet;
using Application.Features.Queries.SubmissionsPeriodGet;
using AutoMapper;
using Contracts.Decisions.Get;
using Contracts.Submission.Create;
using Contracts.Submission.Submit;
using Contracts.Submissions.Get;
using Resolvers;

public class SubmissionProfile : Profile
{
    public SubmissionProfile()
    {
        CreateMap<GetPackagingResubmissionApplicationDetailsRequest, GetPackagingResubmissionApplicationDetailsQuery>();

        CreateMap<GetRegistrationApplicationDetailsRequest, GetRegistrationApplicationDetailsQuery>();

        CreateMap<SubmissionCreateRequest, SubmissionCreateCommand>();

        CreateMap<SubmissionPayload, SubmissionSubmitCommand>();

        CreateMap<RegulatorPoMDecisionSubmissionEventsGetRequest, RegulatorPoMDecisionSubmissionEventsGetQuery>();

        CreateMap<RegulatorRegistrationDecisionSubmissionEventsGetRequest, RegulatorRegistrationDecisionSubmissionEventsGetQuery>();

        CreateMap<RegulatorOrganisationRegistrationDecisionSubmissionEventsGetRequest, RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery>();

        CreateMap<SubmissionsGetRequest, SubmissionsGetQuery>()
            .ForMember(dest => dest.Periods, options =>
                options.MapFrom<SplitStringCommaResolver, string>(src => src.Periods));

        CreateMap<SubmissionGetRequest, SubmissionsPeriodGetQuery>();

        CreateMap<DecisionGetRequest, RegulatorDecisionSubmissionEventGetQuery>();
    }
}