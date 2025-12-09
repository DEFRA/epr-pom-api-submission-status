namespace EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;

using System.Diagnostics.CodeAnalysis;
using Enums;
using SubmissionEvent;

[ExcludeFromCodeCoverage]
public class AntivirusCheckEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.AntivirusCheck;

    public Guid FileId { get; set; }

    public FileType FileType { get; set; }

    public string? FileName { get; set; }

    public Guid? RegistrationSetId { get; set; }
}