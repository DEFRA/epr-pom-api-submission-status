namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

using EPR.SubmissionMicroservice.Data.Enums;

public class BrandValidationEventCreateCommand : AbstractValidationEventCreateCommand
{
    public override EventType Type => EventType.BrandValidation;
}