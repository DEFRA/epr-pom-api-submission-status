using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EPR.SubmissionMicroservice.Application.Converters
{
    public class GuidConverter : CustomCreationConverter<Guid>
    {
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            try
            {
                return serializer.Deserialize<Guid>(reader);
            }
            catch
            {
                return Guid.Empty;
            }
        }

        public override Guid Create(Type objectType)
        {
            return Guid.Empty;
        }
    }
}