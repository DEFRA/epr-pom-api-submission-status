using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class AntivirusResultEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.AntivirusResult;

    public Guid FileId { get; set; }

    public AntivirusScanResult AntivirusScanResult { get; set; }
}