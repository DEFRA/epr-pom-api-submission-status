namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

using Data.Entities.ValidationEventError;
using Data.Enums;

public class RegistrationValidationEventCreateCommand : AbstractValidationEventCreateCommand
{
    public override EventType Type => EventType.Registration;

    public bool RequiresBrandsFile { get; set; }

    public bool RequiresPartnershipsFile { get; set; }

    public bool? HasMaxRowErrors { get; set; }

    public int? RowErrorCount { get; set; }

    public int? OrganisationMemberCount { get; set; }

    public class RegistrationValidationError : AbstractValidationError
    {
        public List<ColumnValidationError> ColumnErrors { get; set; }

        public string OrganisationId { get; set; }

        public string SubsidiaryId { get; set; }
    }

    public class RegistrationValidationWarning : AbstractValidationWarning
    {
        public List<ColumnValidationError> ColumnErrors { get; set; }

        public string OrganisationId { get; set; }

        public string SubsidiaryId { get; set; }
    }
}