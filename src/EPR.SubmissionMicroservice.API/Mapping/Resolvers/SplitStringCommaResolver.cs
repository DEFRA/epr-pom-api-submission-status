namespace EPR.SubmissionMicroservice.API.Mapping.Resolvers;

using AutoMapper;

public class SplitStringCommaResolver : IMemberValueResolver<object, object, string, List<string>>
{
    public List<string> Resolve(object source, object destination, string sourceMember, List<string> destMember, ResolutionContext context)
    {
        return string.IsNullOrEmpty(sourceMember) ? new List<string>() : sourceMember.Split(",").ToList();
    }
}