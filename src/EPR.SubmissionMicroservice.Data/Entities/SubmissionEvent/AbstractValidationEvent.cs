namespace EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;

using System.Diagnostics.CodeAnalysis;
using ValidationEventError;
using ValidationEventWarning;

[ExcludeFromCodeCoverage]
public abstract class AbstractValidationEvent : AbstractSubmissionEvent
{
    public bool? IsValid { get; set; }

    public bool? HasWarnings { get; set; }

    public List<AbstractValidationError> ValidationErrors { get; set; }

    public List<AbstractValidationWarning> ValidationWarnings { get; set; }
}