using System.Diagnostics.CodeAnalysis;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.Common
{
    [ExcludeFromCodeCoverage]
    public class RegistrationValidationIssueGetResponse : AbstractValidationIssueGetResponse
    {
        public List<ColumnValidationError> ColumnErrors { get; set; }

        public string OrganisationId { get; set; }

        public string SubsidiaryId { get; set; }
    }
}