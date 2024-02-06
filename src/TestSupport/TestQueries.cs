using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Enums;

namespace TestSupport;

public static class TestQueries
{
    public static class Submission
    {
        public static AbstractSubmissionGetResponse ValidSubmissionResponse(SubmissionType submissionType)
        {
            return submissionType switch
            {
                SubmissionType.Producer => ValidPomSubmissionResponse(),
                SubmissionType.Registration => ValidRegistrationSubmissionResponse(),
                _ => null
            };
        }

        public static PomSubmissionGetResponse ValidPomSubmissionResponse()
        {
            return new PomSubmissionGetResponse
            {
                Id = Guid.NewGuid(),
                DataSourceType = DataSourceType.File,
                SubmissionType = SubmissionType.Producer,
                SubmissionPeriod = null,
                OrganisationId = default,
                UserId = default,
                Created = default,
                PomDataComplete = false,
                ValidationPass = false,
                PomFileName = null
            };
        }

        public static RegistrationSubmissionGetResponse ValidRegistrationSubmissionResponse()
        {
            return new RegistrationSubmissionGetResponse
            {
                Id = Guid.NewGuid(),
                DataSourceType = DataSourceType.File,
                SubmissionType = SubmissionType.Producer,
                SubmissionPeriod = null,
                OrganisationId = default,
                UserId = default,
                Created = default,
                CompanyDetailsDataComplete = false,
                ValidationPass = false,
                CompanyDetailsFileName = null,
                BrandsFileName = null,
                PartnershipsFileName = null,
                RequiresBrandsFile = false,
                RequiresPartnershipsFile = false,
                OrganisationMemberCount = 10
            };
        }
    }

    public static class ProducerValidation
    {
        public static ProducerValidationIssueGetResponse ValidProducerValidationErrorGetResponse()
        {
            return new ProducerValidationIssueGetResponse
            {
                RowNumber = 1,
                ProducerType = "OL",
                ProducerSize = "L",
                WasteType = "WT",
                DataSubmissionPeriod = "2023-P1",
                SubsidiaryId = "jjHF47",
                PackagingCategory = "PC",
                MaterialType = "MT",
                MaterialSubType = "MST",
                FromHomeNation = "FHN",
                ToHomeNation = "THN",
                QuantityKg = "1",
                QuantityUnits = "1",
                ErrorCodes = new List<string>
                {
                    "01"
                }
            };
        }

        public static ProducerValidationIssueGetResponse ValidProducerValidationWarningGetResponse()
        {
            return new ProducerValidationIssueGetResponse
            {
                RowNumber = 1,
                ProducerType = "OL",
                ProducerSize = "L",
                WasteType = "WT",
                DataSubmissionPeriod = "2023-P1",
                SubsidiaryId = "jjHF47",
                PackagingCategory = "PC",
                MaterialType = "MT",
                MaterialSubType = "MST",
                FromHomeNation = "FHN",
                ToHomeNation = "THN",
                QuantityKg = "1",
                QuantityUnits = "1",
                ErrorCodes = new List<string>
                {
                    "01"
                }
            };
        }
    }

    public static class RegistrationValidation
    {
        public static RegistrationValidationIssueGetResponse ValidRegistrationValidationErrorGetResponse()
        {
            return new RegistrationValidationIssueGetResponse
            {
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "840",
                        ColumnIndex = 3,
                        ColumnName = "trading_name"
                    },
                    new()
                    {
                        ErrorCode = "819",
                        ColumnIndex = 6,
                        ColumnName = "main_activity_sic"
                    },
                },
                OrganisationId = "876",
                SubsidiaryId = "8865",
                RowNumber = 4,
                ErrorCodes = { }
            };
        }
    }
}