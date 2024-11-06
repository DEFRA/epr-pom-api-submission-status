using AutoMapper;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.FileDownload;
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
                    s => (s.ValidationErrors == null || s.ValidationErrors.Count == 0) && (s.Errors == null || s.Errors.Count == 0)))
            .ForMember(
                d => d.ErrorCount,
                opt => opt.MapFrom(
                    s => s.ValidationErrors != null ? s.ValidationErrors.Count : 0))
            .ForMember(
                d => d.WarningCount,
                opt => opt.MapFrom(
                    s => s.ValidationWarnings != null ? s.ValidationWarnings.Count : 0))
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
            .ForMember(x => x.OrganisationMemberCount, c => c.NullSubstitute(0))
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
                opt => opt.MapFrom(x => x.Errors == null || x.Errors.Count == 0));
        CreateMap<PartnerValidationEventCreateCommand, PartnerValidationEvent>()
            .ForMember(
                d => d.IsValid,
                opt => opt.MapFrom(x => x.Errors == null || x.Errors.Count == 0));
        CreateMap<RegulatorRegistrationDecisionEventCreateCommand, RegulatorRegistrationDecisionEvent>();
        CreateMap<FileDownloadCheckEventCreateCommand, FileDownloadCheckEvent>();

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
        CreateMap<RegulatorRegistrationDecisionEvent, RegulatorDecisionGetResponse>()
           .ForMember(o => o.IsResubmissionRequired, m => m.Ignore());
        CreateMap<RegulatorRegistrationDecisionEvent, RegulatorRegistrationDecisionGetResponse>()
            .ForMember(o => o.IsResubmissionRequired, m => m.Ignore());

        CreateMap<RegulatorOrganisationRegistrationDecisionGetResponse, AbstractSubmissionEventGetResponse>();
        CreateMap<RegulatorOrganisationRegistrationDecisionEvent, RegulatorOrganisationRegistrationDecisionGetResponse>()
            .ForMember(o => o.IsResubmissionRequired, m => m.Ignore());
        CreateMap<RegulatorOrganisationRegistrationDecisionEventCreateCommand, RegulatorOrganisationRegistrationDecisionEvent>()
            .ForMember(o => o.Type, m => m.Ignore());
    }
}