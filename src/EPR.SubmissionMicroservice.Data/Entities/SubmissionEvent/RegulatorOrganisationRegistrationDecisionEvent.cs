using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

[ExcludeFromCodeCoverage]
public class RegulatorOrganisationRegistrationDecisionEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.RegulatorRegistrationDecision;

    public RegulatorDecision Decision { get; set; }

    public string? RegistrationReferenceNumber { get; set; }

    public DateTime? DecisionDate { get; set; }

    public string Comments { get; set; }
}
