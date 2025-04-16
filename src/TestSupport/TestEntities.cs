namespace TestSupport;

using EPR.SubmissionMicroservice.Data.Enums;
using AntivirusEventEntity = EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using SubmissionEntity = EPR.SubmissionMicroservice.Data.Entities.Submission;
using SubmissionEventEntity = EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using ValidationEventErrorEntity = EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;

public static class TestEntities
{
    public static class Submission
    {
        public static SubmissionEntity.Submission ValidSubmission(SubmissionType submissionType)
        {
            return new SubmissionEntity.Submission
            {
                Id = Guid.NewGuid(),
                SubmissionType = submissionType,
                SubmissionPeriod = "2022",
                DataSourceType = DataSourceType.File,
                OrganisationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Created = default
            };
        }
    }

    public static class SubmissionEvent
    {
        public static SubmissionEventEntity.AbstractSubmissionEvent ValidSubmissionEvent(EventType eventType)
        {
            return eventType switch
            {
                EventType.AntivirusCheck => ValidAntivirusCheckEvent(),
                EventType.AntivirusResult => ValidAntivirusResultEvent(),
                EventType.CheckSplitter => ValidCheckSplitterValidationEvent(),
                EventType.ProducerValidation => ValidProducerValidationEvent(),
                EventType.Registration => ValidRegistrationValidationEvent(),
                _ => throw new NotImplementedException()
            };
        }

        private static AntivirusEventEntity.AntivirusCheckEvent ValidAntivirusCheckEvent()
        {
            return new AntivirusEventEntity.AntivirusCheckEvent
            {
                SubmissionId = Guid.NewGuid(),
                Errors = null,
                UserId = Guid.NewGuid(),
                FileId = Guid.NewGuid(),
                FileName = null,
                FileType = FileType.Pom,
                Created = default
            };
        }

        private static AntivirusEventEntity.AntivirusResultEvent ValidAntivirusResultEvent()
        {
            return new AntivirusEventEntity.AntivirusResultEvent
            {
                SubmissionId = Guid.NewGuid(),
                Errors = null,
                UserId = Guid.NewGuid(),
                FileId = Guid.NewGuid(),
                AntivirusScanResult = AntivirusScanResult.Success,
                Created = default
            };
        }

        private static SubmissionEventEntity.CheckSplitterValidationEvent ValidCheckSplitterValidationEvent()
        {
            return new SubmissionEventEntity.CheckSplitterValidationEvent
            {
                SubmissionId = Guid.NewGuid(),
                Errors = null,
                UserId = Guid.NewGuid(),
                DataCount = 1,
                Created = default
            };
        }

        private static SubmissionEventEntity.ProducerValidationEvent ValidProducerValidationEvent()
        {
            return new SubmissionEventEntity.ProducerValidationEvent
            {
                SubmissionId = Guid.NewGuid(),
                Errors = null,
                UserId = Guid.NewGuid(),
                ValidationErrors = null,
                ProducerId = "123456",
                Created = default
            };
        }

        private static SubmissionEventEntity.RegistrationValidationEvent ValidRegistrationValidationEvent()
        {
            return new SubmissionEventEntity.RegistrationValidationEvent
            {
                SubmissionId = Guid.NewGuid(),
                Errors = null,
                UserId = Guid.NewGuid(),
                ValidationErrors = null,
                RequiresBrandsFile = true,
                RequiresPartnershipsFile = false,
                OrganisationMemberCount = 10,
                Created = default
            };
        }
    }

    public static class ValidationEventError
    {
        public static ValidationEventErrorEntity.AbstractValidationError ValidValidationEventError(ValidationType validationErrorType)
        {
            return validationErrorType switch
            {
                ValidationType.CheckSplitter => ValidCheckSplitterValidationError(),
                ValidationType.ProducerValidation => ValidProducerValidationError(),
                ValidationType.Registration => ValidRegistrationValidationError(),
                _ => throw new NotImplementedException()
            };
        }

        private static ValidationEventErrorEntity.CheckSplitterValidationError ValidCheckSplitterValidationError()
        {
            return new ValidationEventErrorEntity.CheckSplitterValidationError
            {
                Id = Guid.NewGuid(),
                ValidationEventId = default,
                ValidationErrorType = ValidationType.CheckSplitter,
                RowNumber = 0,
                Created = default,
                ValidationEvent = null
            };
        }

        private static ValidationEventErrorEntity.ProducerValidationError ValidProducerValidationError()
        {
            return new ValidationEventErrorEntity.ProducerValidationError
            {
                Id = Guid.NewGuid(),
                ValidationEventId = default,
                ValidationErrorType = ValidationType.ProducerValidation,
                RowNumber = 0,
                Created = default,
                ValidationEvent = null,
                ProducerType = null,
                ProducerSize = null,
                DataSubmissionPeriod = null,
                SubsidiaryId = null,
                WasteType = null,
                PackagingCategory = null,
                MaterialType = null,
                MaterialSubType = null,
                FromHomeNation = null,
                ToHomeNation = null,
                QuantityKg = null,
                QuantityUnits = null,
                TransitionalPackagingUnits = null,
                RecyclabilityRating = null,
                ErrorCodes = null
            };
        }

        private static ValidationEventErrorEntity.RegistrationValidationError ValidRegistrationValidationError()
        {
            return new ValidationEventErrorEntity.RegistrationValidationError
            {
                Id = Guid.NewGuid(),
                ValidationEventId = default,
                ValidationErrorType = ValidationType.CheckSplitter,
                RowNumber = 0,
                Created = default,
                ValidationEvent = null
            };
        }
    }
}