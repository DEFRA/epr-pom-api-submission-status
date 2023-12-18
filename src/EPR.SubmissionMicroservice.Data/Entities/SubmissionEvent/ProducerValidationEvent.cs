namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

using System.Diagnostics.CodeAnalysis;
using Enums;

[ExcludeFromCodeCoverage]
public class ProducerValidationEvent : AbstractValidationEvent
{
    public override EventType Type => EventType.ProducerValidation;

    public string ProducerId { get; set; }
}