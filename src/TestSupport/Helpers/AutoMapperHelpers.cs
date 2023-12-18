namespace TestSupport.Helpers;

using AutoMapper;

public static class AutoMapperHelpers
{
    public static Mapper GetMapper()
    {
        var profiles = new Profile[]
        {
            new EPR.SubmissionMicroservice.API.Mapping.SubmissionProfile(),
            new EPR.SubmissionMicroservice.Application.Mapping.SubmissionProfile(),
            new EPR.SubmissionMicroservice.Application.Mapping.SubmissionEventProfile(),
            new EPR.SubmissionMicroservice.Application.Mapping.ValidationEventErrorProfile()
        };
        return new Mapper(new MapperConfiguration(config => config.AddProfiles(profiles)));
    }
}