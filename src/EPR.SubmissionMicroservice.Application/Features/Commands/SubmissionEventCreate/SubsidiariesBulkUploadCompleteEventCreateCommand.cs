using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class SubsidiariesBulkUploadCompleteEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.SubsidiariesBulkUploadComplete;

    public string FileName { get; set; }
}