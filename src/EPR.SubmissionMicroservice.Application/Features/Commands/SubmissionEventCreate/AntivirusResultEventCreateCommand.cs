using EPR.SubmissionMicroservice.Application.Converters;
using EPR.SubmissionMicroservice.Data.Enums;
using Newtonsoft.Json;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class AntivirusResultEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.AntivirusResult;

    [JsonConverter(typeof(GuidConverter))]
    public Guid FileId { get; set; }

    public AntivirusScanResult AntivirusScanResult { get; set; }

    public AntivirusScanTrigger? AntivirusScanTrigger { get; set; }

    public bool? RequiresRowValidation { get; set; }
}