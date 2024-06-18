using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

[ExcludeFromCodeCoverage]
public class RegulatorRegistrationDecisionEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.RegulatorRegistrationDecision;

    public RegulatorDecision Decision { get; set; }

    public string Comments { get; set; }

    public Guid FileId { get; set; }
}