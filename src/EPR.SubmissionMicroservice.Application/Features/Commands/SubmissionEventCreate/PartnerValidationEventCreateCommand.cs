namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

using EPR.SubmissionMicroservice.Data.Enums;

public class PartnerValidationEventCreateCommand : AbstractValidationEventCreateCommand
{
    public override EventType Type => EventType.PartnerValidation;
}