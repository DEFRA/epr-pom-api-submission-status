using AutoMapper;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;

namespace EPR.SubmissionMicroservice.Application.Mapping;

public class SubmissionEventProfile : Profile
{
    public SubmissionEventProfile()
    {
        CreateMap<AbstractSubmissionEventCreateCommand, AbstractSubmissionEvent>()
            .ForMember(o => o.Created, m => m.Ignore())
            .ForMember(o => o.Id, m => m.Ignore())
            .IncludeAllDerived();
        CreateMap<AbstractValidationEventCreateCommand, AbstractValidationEvent>()
            .ForMember(
                d => d.IsValid,
                opt => opt.MapFrom(
                    s => (s.ValidationErrors == null || !s.ValidationErrors.Any()) && (s.Errors == null || !s.Errors.Any())))
            .ForMember(
                d => d.HasWarnings,
                opt => opt.MapFrom(
                    s => (s.ValidationWarnings != null && s.ValidationWarnings.Any())))
            .IncludeAllDerived();
        CreateMap<CheckSplitterValidationEventCreateCommand, CheckSplitterValidationEvent>();
        CreateMap<ProducerValidationEventCreateCommand, ProducerValidationEvent>();
        CreateMap<AntivirusCheckEventCreateCommand, AntivirusCheckEvent>();
        CreateMap<AntivirusResultEventCreateCommand, AntivirusResultEvent>()
            .ForMember(x => x.RequiresRowValidation, c => c.NullSubstitute(false));
        CreateMap<RegulatorPoMDecisionEventCreateCommand, RegulatorPoMDecisionEvent>();

        CreateMap<RegistrationValidationEventCreateCommand, RegistrationValidationEvent>()
            .ForMember(x => x.HasMaxRowErrors, c => c.NullSubstitute(false))
            .ForMember(x => x.RowErrorCount, c => c.NullSubstitute(0))
            .AfterMap((command, e) =>
            {
                foreach (var validationError in e.ValidationErrors)
                {
                    validationError.BlobName = command.BlobName;
                }
            });
        CreateMap<BrandValidationEventCreateCommand, BrandValidationEvent>()
            .ForMember(
                d => d.IsValid,
                opt => opt.MapFrom(x => x.Errors == null || !x.Errors.Any()));
        CreateMap<PartnerValidationEventCreateCommand, PartnerValidationEvent>()
            .ForMember(
                d => d.IsValid,
                opt => opt.MapFrom(x => x.Errors == null || !x.Errors.Any()));
        CreateMap<RegulatorRegistrationDecisionEventCreateCommand, RegulatorRegistrationDecisionEvent>();

        CreateMap<AbstractValidationEventCreateCommand.AbstractValidationError, AbstractValidationError>()
            .ForMember(o => o.Created, m => m.Ignore())
            .ForMember(o => o.Id, m => m.Ignore())
            .ForMember(o => o.ValidationEvent, m => m.Ignore())
            .ForMember(o => o.ValidationEventId, m => m.Ignore())
            .IncludeAllDerived();
        CreateMap<AbstractValidationEventCreateCommand.AbstractValidationWarning, AbstractValidationWarning>()
            .ForMember(o => o.Created, m => m.Ignore())
            .ForMember(o => o.Id, m => m.Ignore())
            .ForMember(o => o.ValidationEvent, m => m.Ignore())
            .ForMember(o => o.ValidationEventId, m => m.Ignore())
            .IncludeAllDerived();
        CreateMap<CheckSplitterValidationEventCreateCommand.CheckSplitterValidationError, CheckSplitterValidationError>();
        CreateMap<CheckSplitterValidationEventCreateCommand.CheckSplitterValidationWarning, CheckSplitterValidationWarning>();
        CreateMap<ProducerValidationEventCreateCommand.ProducerValidationError, ProducerValidationError>();
        CreateMap<ProducerValidationEventCreateCommand.ProducerValidationWarning, ProducerValidationWarning>();
        CreateMap<RegistrationValidationEventCreateCommand.RegistrationValidationError, RegistrationValidationError>();
        CreateMap<RegulatorPoMDecisionEvent, RegulatorDecisionGetResponse>();
        CreateMap<RegulatorRegistrationDecisionEvent, RegulatorRegistrationDecisionGetResponse>()
            .ForMember(o => o.IsResubmissionRequired, m => m.Ignore());
    }
}