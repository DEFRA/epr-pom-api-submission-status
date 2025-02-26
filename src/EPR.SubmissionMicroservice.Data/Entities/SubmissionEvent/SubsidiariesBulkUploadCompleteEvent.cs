using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

[ExcludeFromCodeCoverage]
public class SubsidiariesBulkUploadCompleteEvent : AbstractSubmissionEvent
{
    public override EventType Type => EventType.SubsidiariesBulkUploadComplete;

    public string FileName { get; set; }
}