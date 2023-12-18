namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

[ExcludeFromCodeCoverage]
public class BrandValidationEvent : AbstractValidationEvent
{
    public override EventType Type => EventType.BrandValidation;
}