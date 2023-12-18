namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

using Data.Enums;

public class CheckSplitterValidationEventCreateCommand : AbstractValidationEventCreateCommand
{
    public override EventType Type => EventType.CheckSplitter;

    public int DataCount { get; set; }

    public class CheckSplitterValidationError : AbstractValidationError
    {
    }
}
