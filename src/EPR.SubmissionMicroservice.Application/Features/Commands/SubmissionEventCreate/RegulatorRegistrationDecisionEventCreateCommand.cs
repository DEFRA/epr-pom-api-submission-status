using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class RegulatorRegistrationDecisionEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.RegulatorRegistrationDecision;

    public RegulatorDecision Decision { get; set; }

    public string Comments { get; set; }

    public Guid FileId { get; set; }
}