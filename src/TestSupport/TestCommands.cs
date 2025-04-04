﻿namespace TestSupport;

using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionCreate;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Enums;

public static class TestCommands
{
    public static class Submission
    {
        public static SubmissionCreateCommand ValidSubmissionCreateCommand(SubmissionType submissionType)
        {
            return new SubmissionCreateCommand
            {
                Id = Guid.NewGuid(),
                SubmissionType = submissionType,
                DataSourceType = DataSourceType.File,
                SubmissionPeriod = "2022",
                OrganisationId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };
        }

        public static SubmissionCreateCommand InvalidSubmissionCreateCommand(SubmissionType submissionType)
        {
            return new SubmissionCreateCommand
            {
                 Id = Guid.Empty,
                 DataSourceType = 0,
                 SubmissionPeriod = string.Empty,
                 SubmissionType = 0,
                 OrganisationId = Guid.Empty,
                 UserId = Guid.Empty
            };
        }
    }

    public static class SubmissionEvent
    {
        public const string BlobName = "blobName";
        public const string BlobContainerName = "blob-container-name";

        public static AbstractSubmissionEventCreateCommand ValidSubmissionEventCreateCommand(EventType eventType)
        {
            return eventType switch
            {
                EventType.AntivirusCheck => ValidAntivirusCheckEventCreateCommand(),
                EventType.CheckSplitter => ValidCheckSplitterValidationEventCreateCommand(),
                EventType.ProducerValidation => ValidProducerValidationEventCreateCommand(),
                EventType.Registration => ValidRegistrationValidationEventCreateCommand(),
                EventType.AntivirusResult => ValidAntivirusResultEventUploadCreateCommand(),
                EventType.RegulatorPoMDecision => ValidRegulatorPoMDecisionEventCreateCommand(),
                EventType.FileDownloadCheck => ValidCheckFileDownloadCheckEventCreateCommand(),
                EventType.RegistrationFeePayment => ValidRegistrationFeePaymentEventCreateCommand(),
                EventType.RegulatorRegistrationDecision => ValidRegulatorRegistrationDecisionEventCreateCommand(),
                EventType.PackagingDataResubmissionFeePayment => ValidPackagingDataResubmissionFeePaymentEventCreateCommand(),
                _ => throw new NotImplementedException()
            };
        }

        public static AntivirusCheckEventCreateCommand ValidAntivirusCheckEventCreateCommand()
        {
            return new AntivirusCheckEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                Errors = null,
                UserId = Guid.NewGuid(),
                FileId = Guid.NewGuid(),
                FileName = "PomFileName.csv",
                FileType = FileType.Pom,
                BlobName = BlobName,
                BlobContainerName = BlobContainerName
            };
        }

        public static AntivirusResultEventCreateCommand ValidAntivirusResultEventUploadCreateCommand()
        {
            return new AntivirusResultEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                Errors = null,
                UserId = Guid.NewGuid(),
                FileId = Guid.NewGuid(),
                AntivirusScanResult = AntivirusScanResult.Success,
                AntivirusScanTrigger = AntivirusScanTrigger.Upload
            };
        }

        public static AntivirusResultEventCreateCommand ValidAntivirusResultEventDownloadCreateCommand()
        {
            return new AntivirusResultEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                Errors = null,
                UserId = Guid.NewGuid(),
                FileId = Guid.NewGuid(),
                AntivirusScanResult = AntivirusScanResult.Started,
                AntivirusScanTrigger = AntivirusScanTrigger.Download,
                BlobName = BlobName,
                BlobContainerName = BlobContainerName
            };
        }

        public static AntivirusResultEventCreateCommand ValidAntivirusResultEventCreateCommandWithRequiresRowValidation()
        {
            return new AntivirusResultEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                Errors = null,
                UserId = Guid.NewGuid(),
                FileId = Guid.NewGuid(),
                AntivirusScanResult = AntivirusScanResult.Success
            };
        }

        public static CheckSplitterValidationEventCreateCommand ValidCheckSplitterValidationEventCreateCommand()
        {
            return new CheckSplitterValidationEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DataCount = 1,
                BlobName = BlobName,
                BlobContainerName = BlobContainerName
            };
        }

        public static FileDownloadCheckEventCreateCommand ValidCheckFileDownloadCheckEventCreateCommand()
        {
            return new FileDownloadCheckEventCreateCommand
            {
                 SubmissionId = Guid.NewGuid(),
                 UserId = Guid.NewGuid(),
                 UserEmail = "fake@fake.com",
                 ContentScan = "file",
                 FileId = Guid.NewGuid(),
                 FileName = "fake.cv",
                 SubmissionType = SubmissionType.Registration
            };
        }

        public static ProducerValidationEventCreateCommand ValidProducerValidationEventCreateCommand()
        {
            return new ProducerValidationEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ProducerId = "123456",
                BlobName = BlobName,
                BlobContainerName = BlobContainerName
            };
        }

        public static RegistrationValidationEventCreateCommand ValidRegistrationValidationEventCreateCommand()
        {
            return new RegistrationValidationEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                RequiresBrandsFile = false,
                RequiresPartnershipsFile = false,
                OrganisationMemberCount = 10
            };
        }

        public static BrandValidationEventCreateCommand ValidBrandValidationEventCreateCommand()
        {
            return new BrandValidationEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                BlobName = "the-file.csv",
                BlobContainerName = "my-container",
            };
        }

        public static PartnerValidationEventCreateCommand ValidPartnerValidationEventCreateCommand()
        {
            return new PartnerValidationEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                BlobName = "the-file.csv",
                BlobContainerName = "my-container",
            };
        }

        public static RegulatorPoMDecisionEventCreateCommand ValidRegulatorPoMDecisionEventCreateCommand()
        {
            return new RegulatorPoMDecisionEventCreateCommand()
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FileId = Guid.NewGuid(),
                Decision = RegulatorDecision.Accepted,
                Comments = string.Empty,
                IsResubmissionRequired = false
            };
        }

        public static RegulatorRegistrationDecisionEventCreateCommand ValidRegulatorRegistrationDecisionEventCreateCommand()
        {
            return new RegulatorRegistrationDecisionEventCreateCommand()
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FileId = Guid.NewGuid(),
                Decision = RegulatorDecision.Accepted,
                Comments = string.Empty,
            };
        }

        public static RegistrationFeePaymentEventCreateCommand ValidRegistrationFeePaymentEventCreateCommand()
        {
            return new RegistrationFeePaymentEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                PaidAmount = "£100.00",
                PaymentMethod = "Offline",
                PaymentStatus = "Paid"
            };
        }

        public static PackagingDataResubmissionFeePaymentEventCreateCommand ValidPackagingDataResubmissionFeePaymentEventCreateCommand()
        {
            return new PackagingDataResubmissionFeePaymentEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                PaidAmount = "£50.00",
                PaymentMethod = "Offline",
                PaymentStatus = "Paid",
                ReferenceNumber = "TestRef1234",
                ComplianceSchemeId = Guid.NewGuid()
            };
        }

        public static RegistrationApplicationSubmittedEventCreateCommand ValidRegistrationApplicationSubmittedEventCreateCommand()
        {
            return new RegistrationApplicationSubmittedEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Comments = string.Empty,
                ApplicationReferenceNumber = "PEPR00002125P1",
                SubmissionDate = DateTime.Today,
            };
        }

        public static SubsidiariesBulkUploadCompleteEventCreateCommand ValidSubsidiariesBulkUploadCompleteEventCreateCommand()
        {
            return new SubsidiariesBulkUploadCompleteEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FileName = "SubsidiaryFileName.csv"
            };
        }

        public static CheckSplitterValidationEventCreateCommand InvalidCheckSplitterValidationEventCreateCommand()
        {
            return new CheckSplitterValidationEventCreateCommand
            {
                SubmissionId = Guid.Empty,
                Type = 0,
                UserId = null,
                DataCount = 0,
                BlobName = null,
                BlobContainerName = null
            };
        }

        public static CheckSplitterValidationEventCreateCommand InvalidCheckSplitterValidationWithErrorsEventCreateCommand()
        {
            return new CheckSplitterValidationEventCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DataCount = 1,
                BlobName = BlobName,
                BlobContainerName = BlobContainerName,
                ValidationErrors = new List<AbstractValidationEventCreateCommand.AbstractValidationError> { CreateValidationError(1), CreateValidationError(2) }
            };

            CheckSplitterValidationEventCreateCommand.CheckSplitterValidationError CreateValidationError(int row)
            {
                return new CheckSplitterValidationEventCreateCommand.CheckSplitterValidationError
                {
                    BlobName = $"Blob file {row}",
                    RowNumber = row,
                    ValidationErrorType = ValidationType.CheckSplitter,
                    SubsidiaryId = "1",
                    ErrorCodes = new List<string>() { "ER" + row }
                };
            }
        }

        public static ProducerValidationEventCreateCommand InvalidProducerValidationEventCreateCommand()
        {
            return new ProducerValidationEventCreateCommand
            {
                SubmissionId = Guid.Empty,
                Type = 0,
                UserId = null,
                ProducerId = "123456",
                BlobName = null,
                BlobContainerName = null
            };
        }

        public static RegistrationValidationEventCreateCommand InvalidRegistrationValidationEventCreateCommand()
        {
            return new RegistrationValidationEventCreateCommand
            {
                SubmissionId = Guid.Empty,
                Type = 0,
                UserId = null,
                RequiresBrandsFile = false,
                RequiresPartnershipsFile = false,
                BlobName = null,
                BlobContainerName = null,
                RowErrorCount = 6,
                HasMaxRowErrors = false,
                OrganisationMemberCount = null,
                ValidationErrors = new()
                {
                    CreateValidationError(1),
                    CreateValidationError(2),
                    CreateValidationError(3),
                    CreateValidationError(4),
                    CreateValidationError(5),
                    CreateValidationError(6)
                }
            };

            RegistrationValidationEventCreateCommand.RegistrationValidationError CreateValidationError(int row)
            {
                return new RegistrationValidationEventCreateCommand.RegistrationValidationError
                {
                    BlobName = $"Blob file {row}",
                    RowNumber = row,
                    ValidationErrorType = ValidationType.Registration,
                    OrganisationId = "123",
                    SubsidiaryId = "1",
                    ColumnErrors = new()
                    {
                        new ColumnValidationError { ColumnIndex = 0, ColumnName = "organisation_id", ErrorCode = "801" }
                    }
                };
            }
        }

        public static RegistrationValidationEventCreateCommand RegistrationValidationEventCreateCommandWithWarnings()
        {
            return new RegistrationValidationEventCreateCommand
            {
                SubmissionId = Guid.Empty,
                Type = 0,
                UserId = null,
                RequiresBrandsFile = false,
                RequiresPartnershipsFile = false,
                BlobName = null,
                BlobContainerName = null,
                RowErrorCount = 0,
                HasMaxRowErrors = false,
                OrganisationMemberCount = null,
                ValidationWarnings = new()
                {
                    CreateValidationWarning(1)
                }
            };

            RegistrationValidationEventCreateCommand.RegistrationValidationWarning CreateValidationWarning(int row)
            {
                return new RegistrationValidationEventCreateCommand.RegistrationValidationWarning
                {
                    BlobName = $"Blob file {row}",
                    RowNumber = row,
                    ValidationWarningType = ValidationType.Registration,
                    OrganisationId = "123",
                    SubsidiaryId = "1",
                    ColumnErrors = new()
                    {
                        new ColumnValidationError { ColumnIndex = 0, ColumnName = "organisation_id", ErrorCode = "73" }
                    }
                };
            }
        }

        public static BrandValidationEventCreateCommand InvalidBrandValidationEventCreateCommand(params string[] errorCodes)
        {
            return new BrandValidationEventCreateCommand
            {
                SubmissionId = Guid.Empty,
                Type = EventType.BrandValidation,
                Errors = errorCodes.ToList(),
                UserId = null,
                BlobName = null,
                BlobContainerName = null,
            };
        }

        public static PartnerValidationEventCreateCommand InvalidPartnerValidationEventCreateCommand(params string[] errorCodes)
        {
            return new PartnerValidationEventCreateCommand
            {
                SubmissionId = Guid.Empty,
                Type = EventType.PartnerValidation,
                Errors = errorCodes.ToList(),
                UserId = null,
                BlobName = null,
                BlobContainerName = null,
            };
        }

        public static PackagingResubmissionReferenceNumberCreateCommand ValidPackagingResubmissionReferenceNumberCreatedCommand()
        {
            return new PackagingResubmissionReferenceNumberCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                PackagingResubmissionReferenceNumber = "test-ref"
            };
        }

        public static PackagingResubmissionFeeViewCreateCommand ValidPackagingResubmissionFeeViewCreateCommand()
        {
            return new PackagingResubmissionFeeViewCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                IsPackagingResubmissionFeeViewed = true
            };
        }

        public static PackagingResubmissionApplicationSubmittedCreateCommand ValidPackagingResubmissionSubmittedCreateCommand()
        {
            return new PackagingResubmissionApplicationSubmittedCreateCommand
            {
                SubmissionId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                IsResubmitted = true
            };
        }
    }
}