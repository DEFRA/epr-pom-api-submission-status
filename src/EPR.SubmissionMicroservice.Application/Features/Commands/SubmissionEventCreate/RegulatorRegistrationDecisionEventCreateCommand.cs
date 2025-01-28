using EPR.SubmissionMicroservice.Data.Enums;
using Newtonsoft.Json;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class RegulatorRegistrationDecisionEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.RegulatorRegistrationDecision;

    public RegulatorDecision Decision { get; set; }

    public string? Comments { get; set; }

    public Guid? FileId { get; set; }

    public bool IsForOrganisationRegistration { get; set; }

    [JsonProperty("applicationReferenceNumber")]
    public string? AppReferenceNumber { get; set; }

    public string? RegistrationReferenceNumber { get; set; }

    public DateTime? DecisionDate { get; set; }
}