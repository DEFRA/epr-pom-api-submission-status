namespace EPR.SubmissionMicroservice.Application.Mapping;

using AutoMapper;
using Data.Entities.Submission;
using Features.Commands.SubmissionCreate;
using Features.Queries.Common;

public class SubmissionProfile : Profile
{
    public SubmissionProfile()
    {
        CreateMap<SubmissionCreateCommand, Submission>();

        CreateMap<Submission, AbstractSubmissionGetResponse>()
            .IncludeAllDerived();
        CreateMap<Submission, PomSubmissionGetResponse>();
        CreateMap<Submission, RegistrationSubmissionGetResponse>();
        CreateMap<Submission, SubsidiarySubmissionGetResponse>();
        CreateMap<Submission, CompaniesHouseSubmissionGetResponse>();

        CreateMap<Submission, SubmissionGetResponse>()
            .ForMember(dest => dest.SubmissionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Year, opt => opt.MapFrom(src => src.Created.Year));
    }
}