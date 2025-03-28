﻿namespace EPR.SubmissionMicroservice.Application.Converters;

using Data.Enums;
using Features.Commands.SubmissionEventCreate;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

public class EventConverter : CustomCreationConverter<AbstractSubmissionEventCreateCommand>
{
    private EventType _eventType;

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var readerContent = (JObject)JToken.ReadFrom(reader);
        _eventType = (EventType)readerContent.GetValue("type", StringComparison.OrdinalIgnoreCase).Value<int>();
        return base.ReadJson(readerContent.CreateReader(), objectType, existingValue, serializer);
    }

    public override AbstractSubmissionEventCreateCommand Create(Type objectType)
    {
        return _eventType switch
        {
            EventType.CheckSplitter => new CheckSplitterValidationEventCreateCommand(),
            EventType.ProducerValidation => new ProducerValidationEventCreateCommand(),
            EventType.Registration => new RegistrationValidationEventCreateCommand(),
            EventType.BrandValidation => new BrandValidationEventCreateCommand(),
            EventType.PartnerValidation => new PartnerValidationEventCreateCommand(),
            EventType.AntivirusCheck => new AntivirusCheckEventCreateCommand(),
            EventType.AntivirusResult => new AntivirusResultEventCreateCommand(),
            EventType.RegulatorPoMDecision => new RegulatorPoMDecisionEventCreateCommand(),
            EventType.RegulatorRegistrationDecision => new RegulatorRegistrationDecisionEventCreateCommand(),
            EventType.FileDownloadCheck => new FileDownloadCheckEventCreateCommand(),
            EventType.RegistrationFeePayment => new RegistrationFeePaymentEventCreateCommand(),
            EventType.RegistrationApplicationSubmitted => new RegistrationApplicationSubmittedEventCreateCommand(),
            EventType.PackagingDataResubmissionFeePayment => new PackagingDataResubmissionFeePaymentEventCreateCommand(),
            EventType.SubsidiariesBulkUploadComplete => new SubsidiariesBulkUploadCompleteEventCreateCommand(),
            EventType.PackagingResubmissionReferenceNumberCreated => new PackagingResubmissionReferenceNumberCreateCommand(),
            EventType.PackagingResubmissionFeeViewed => new PackagingResubmissionFeeViewCreateCommand(),
            EventType.PackagingResubmissionApplicationSubmitted => new PackagingResubmissionApplicationSubmittedCreateCommand(),
            _ => throw new NotImplementedException()
        };
    }
}