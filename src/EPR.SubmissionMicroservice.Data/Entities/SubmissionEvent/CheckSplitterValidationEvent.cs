namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

using System.Diagnostics.CodeAnalysis;
using Enums;

[ExcludeFromCodeCoverage]
public class CheckSplitterValidationEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.CheckSplitter;

    public int DataCount { get; set; }
}