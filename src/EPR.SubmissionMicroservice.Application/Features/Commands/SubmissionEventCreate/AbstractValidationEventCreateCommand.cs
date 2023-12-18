using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

public abstract class AbstractValidationEventCreateCommand : AbstractSubmissionEventCreateCommand
{
    public List<AbstractValidationError> ValidationErrors { get; set; } = new();

    public List<AbstractValidationWarning> ValidationWarnings { get; set; } = new();

    public abstract class AbstractValidationError
    {
        public ValidationType ValidationErrorType { get; set; }

        public int RowNumber { get; set; }

        public string? BlobName { get; set; }

        public List<string> ErrorCodes { get; set; }
    }

    public abstract class AbstractValidationWarning
    {
        public ValidationType ValidationWarningType { get; set; }

        public int RowNumber { get; set; }

        public string? BlobName { get; set; }

        public List<string> ErrorCodes { get; set; }
    }
}