namespace EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class RegistrationValidationError : AbstractValidationError
{
    public List<ColumnValidationError> ColumnErrors { get; set; }

    public string OrganisationId { get; set; }

    public string SubsidiaryId { get; set; }
}