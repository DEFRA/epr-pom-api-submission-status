using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;

namespace EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;

public class RegistrationValidationWarning : AbstractValidationWarning
{
    public List<ColumnValidationError> ColumnErrors { get; set; }

    public string OrganisationId { get; set; }

    public string SubsidiaryId { get; set; }
}
