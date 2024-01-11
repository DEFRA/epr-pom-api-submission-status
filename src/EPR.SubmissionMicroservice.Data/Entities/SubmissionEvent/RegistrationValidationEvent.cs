namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

using System.Diagnostics.CodeAnalysis;
using Enums;

[ExcludeFromCodeCoverage]
public class RegistrationValidationEvent : AbstractValidationEvent
{
    public override EventType Type => EventType.Registration;

    public bool RequiresBrandsFile { get; set; }

    public bool RequiresPartnershipsFile { get; set; }
}