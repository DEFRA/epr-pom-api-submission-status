namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

[ExcludeFromCodeCoverage]
public class PartnerValidationEvent : AbstractValidationEvent
{
    public override EventType Type => EventType.PartnerValidation;
}