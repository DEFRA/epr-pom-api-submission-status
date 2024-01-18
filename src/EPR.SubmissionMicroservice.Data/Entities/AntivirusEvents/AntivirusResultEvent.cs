namespace EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;

using System.Diagnostics.CodeAnalysis;
using Enums;
using SubmissionEvent;

[ExcludeFromCodeCoverage]
public class AntivirusResultEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.AntivirusResult;

    public Guid FileId { get; set; }

    public AntivirusScanResult AntivirusScanResult { get; set; }

    public bool? RequiresRowValidation { get; set; }
}