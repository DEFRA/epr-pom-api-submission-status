using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public class AntivirusCheckEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public override EventType Type => EventType.AntivirusCheck;

    public Guid FileId { get; set; }

    public FileType FileType { get; set; }

    public string FileName { get; set; }

    public Guid? RegistrationSetId { get; set; }
}