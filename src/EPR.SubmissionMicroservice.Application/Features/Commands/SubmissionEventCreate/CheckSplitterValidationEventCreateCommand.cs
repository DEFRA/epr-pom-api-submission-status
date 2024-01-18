namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

using Data.Enums;

public class CheckSplitterValidationEventCreateCommand : AbstractValidationEventCreateCommand
{
    public override EventType Type => EventType.CheckSplitter;

    public int DataCount { get; set; }

    public class CheckSplitterValidationError : AbstractValidationError
    {
    }

    public class CheckSplitterValidationWarning : AbstractValidationWarning
    {
        public string? ProducerId { get; set; }

        public string? ProducerType { get; set; }

        public string? ProducerSize { get; set; }

        public string? WasteType { get; set; }

        public string? SubsidiaryId { get; set; }

        public string? DataSubmissionPeriod { get; set; }

        public string? PackagingCategory { get; set; }

        public string? MaterialType { get; set; }

        public string? MaterialSubType { get; set; }

        public string? FromHomeNation { get; set; }

        public string? ToHomeNation { get; set; }

        public string? QuantityKg { get; set; }

        public string? QuantityUnits { get; set; }
    }
}