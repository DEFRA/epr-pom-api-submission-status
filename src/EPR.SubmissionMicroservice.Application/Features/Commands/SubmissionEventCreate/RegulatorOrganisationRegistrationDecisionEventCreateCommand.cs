using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class RegulatorOrganisationRegistrationDecisionEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.RegulatorRegistrationDecision;

    public RegulatorDecision Decision { get; set; }

    public string? RegistrationReferenceNumber { get; set; }

    public DateTime? DecisionDate { get; set; }

    public string? Comments { get; set; }
}
