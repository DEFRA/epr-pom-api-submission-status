using AutoMapper;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;

namespace EPR.SubmissionMicroservice.Application.Mapping;

public class ValidationEventErrorProfile : Profile
{
    public ValidationEventErrorProfile()
    {
        CreateMap<AbstractValidationError, AbstractValidationIssueGetResponse>()
            .IncludeAllDerived();
        CreateMap<ProducerValidationError, ProducerValidationIssueGetResponse>();
        CreateMap<CheckSplitterValidationError, ProducerValidationIssueGetResponse>();
        CreateMap<RegistrationValidationError, RegistrationValidationIssueGetResponse>();

        CreateMap<AbstractValidationWarning, AbstractValidationIssueGetResponse>()
            .IncludeAllDerived();
        CreateMap<ProducerValidationWarning, ProducerValidationIssueGetResponse>();
        CreateMap<CheckSplitterValidationWarning, ProducerValidationIssueGetResponse>();
    }
}