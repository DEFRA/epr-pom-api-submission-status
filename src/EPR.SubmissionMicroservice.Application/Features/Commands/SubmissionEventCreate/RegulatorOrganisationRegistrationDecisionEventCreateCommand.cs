using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class RegulatorOrganisationRegistrationDecisionEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.RegulatorRegistrationDecision;

    public RegulatorDecision Decision { get; set; }

    /// <summary>
    /// Gets or sets the Comments value.
    /// This is required for the Regulator Organisation Registration Journey.
    /// </summary>
    /// <value>An optional string containing Regulator (or other) comments.</value>
    public string? Comments { get; set; }
}
