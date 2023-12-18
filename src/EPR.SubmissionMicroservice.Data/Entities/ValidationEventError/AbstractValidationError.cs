namespace EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;

using System.Diagnostics.CodeAnalysis;
using Common.Functions.Database.Entities.Interfaces;
using Enums;
using SubmissionEvent;

[ExcludeFromCodeCoverage]
public abstract class AbstractValidationError : EntityWithId, ICreated
{
    public Guid ValidationEventId { get; set; }

    public string BlobName { get; set; }

    public ValidationType ValidationErrorType { get; set; }

    public int RowNumber { get; set; }

    public List<string> ErrorCodes { get; set; }

    public DateTime Created { get; set; }

    public AbstractValidationEvent ValidationEvent { get; set; }
}