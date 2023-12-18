using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using EPR.SubmissionMicroservice.Data.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace EPR.SubmissionMicroservice.Application.Converters;

public class EventErrorConverter : CustomCreationConverter<AbstractValidationEventCreateCommand.AbstractValidationError>
{
    private ValidationType _validationErrorType;

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var readerContent = (JObject)JToken.ReadFrom(reader);
        _validationErrorType = (ValidationType)readerContent.GetValue("validationErrorType", StringComparison.OrdinalIgnoreCase).Value<int>();
        return base.ReadJson(readerContent.CreateReader(), objectType, existingValue, serializer);
    }

    public override AbstractValidationEventCreateCommand.AbstractValidationError Create(Type objectType)
    {
        return _validationErrorType switch
        {
            ValidationType.CheckSplitter =>
                new CheckSplitterValidationEventCreateCommand.CheckSplitterValidationError(),
            ValidationType.ProducerValidation =>
                new ProducerValidationEventCreateCommand.ProducerValidationError(),
            ValidationType.Registration =>
                new RegistrationValidationEventCreateCommand.RegistrationValidationError(),
            _ => throw new NotImplementedException()
        };
    }
}