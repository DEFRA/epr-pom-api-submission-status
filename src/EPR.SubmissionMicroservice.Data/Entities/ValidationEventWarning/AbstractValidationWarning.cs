using System.Diagnostics.CodeAnalysis;
using EPR.Common.Functions.Database.Entities.Interfaces;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;

[ExcludeFromCodeCoverage]
public abstract class AbstractValidationWarning : EntityWithId, ICreated
{
    public Guid ValidationEventId { get; set; }

    public string BlobName { get; set; }

    public ValidationType ValidationWarningType { get; set; }

    public int RowNumber { get; set; }

    public List<string> ErrorCodes { get; set; }

    public DateTime Created { get; set; }

    public AbstractValidationEvent ValidationEvent { get; set; }
}