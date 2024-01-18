using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

[ExcludeFromCodeCoverage]
public class RegistrationValidationEvent : AbstractValidationEvent
{
    public override EventType Type => EventType.Registration;

    public bool RequiresBrandsFile { get; set; }

    public bool RequiresPartnershipsFile { get; set; }

    public bool? HasMaxRowErrors { get; set; }

    public int? RowErrorCount { get; set; } = 0;
}