using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using EPR.SubmissionMicroservice.Data.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace EPR.SubmissionMicroservice.Application.Converters;

public class EventWarningConverter : CustomCreationConverter<AbstractValidationEventCreateCommand.AbstractValidationWarning>
{
    private ValidationType _validationWarningType;

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var readerContent = (JObject)JToken.ReadFrom(reader);
        _validationWarningType = (ValidationType)readerContent.GetValue("validationWarningType", StringComparison.OrdinalIgnoreCase).Value<int>();
        return base.ReadJson(readerContent.CreateReader(), objectType, existingValue, serializer);
    }

    public override AbstractValidationEventCreateCommand.AbstractValidationWarning Create(Type objectType)
    {
        return _validationWarningType switch
        {
            ValidationType.ProducerValidation =>
                new ProducerValidationEventCreateCommand.ProducerValidationWarning(),
            _ => throw new NotImplementedException()
        };
    }
}