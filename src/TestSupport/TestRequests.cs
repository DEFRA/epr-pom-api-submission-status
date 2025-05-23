﻿namespace TestSupport;

using EPR.SubmissionMicroservice.API.Contracts.Submission.Create;
using EPR.SubmissionMicroservice.Data.Enums;
using Newtonsoft.Json.Linq;

public static class TestRequests
{
    public static class Submission
    {
        public static SubmissionCreateRequest ValidSubmissionCreateRequest(SubmissionType submissionType)
        {
            return new SubmissionCreateRequest
            {
                Id = Guid.NewGuid(),
                SubmissionType = submissionType,
                DataSourceType = DataSourceType.File,
                SubmissionPeriod = "2022"
            };
        }
    }

    public static class SubmissionEvent
    {
        public static JObject ValidSubmissionEventCreateRequest(EventType eventType)
        {
            return eventType switch
            {
                EventType.AntivirusCheck => ValidAntivirusCheckEventCreateRequest(),
                EventType.AntivirusResult => ValidAntivirusResultEventCreateRequest(),
                EventType.CheckSplitter => ValidCheckSplitterValidationEventCreateRequest(),
                EventType.ProducerValidation => ValidProducerValidationEventCreateRequest(),
                EventType.Registration => ValidRegistrationValidationEventCreateRequest(),
                EventType.RegulatorPoMDecision => ValidRegulatorPoMDecisionEventCreateRequest(),
                EventType.RegulatorRegistrationDecision => ValidRegulatorRegistrationDecisionEventCreateRequest(),
                EventType.RegistrationFeePayment => ValidRegistrationFeePaymentEventCreateRequest(),
                EventType.PackagingDataResubmissionFeePayment => ValidPackagingDataResubmissionFeePaymentCreateRequest(),
                _ => throw new NotImplementedException()
            };
        }

        public static JObject ValidAntivirusCheckEventCreateRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.AntivirusCheck,
                fileId = new Guid("b003af79-74c8-4b15-adda-3ada39442ed9"),
                fileName = "MyFile.csv",
                fileType = FileType.Pom
            });
        }

        public static JObject ValidAntivirusResultEventCreateRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.AntivirusResult,
                antivirusScanResult = AntivirusScanResult.Success,
                antivirusScanTrigger = AntivirusScanTrigger.Upload,
                fileId = new Guid("95b3799a-2758-4124-868a-0a8a50ebdea5")
            });
        }

        public static JObject ValidAntivirusResultEventCreateRequestWithRequiresRowValidation()
        {
            return JObject.FromObject(new
            {
                type = EventType.AntivirusResult,
                antivirusScanResult = AntivirusScanResult.Success,
                fileId = new Guid("95b3799a-2758-4124-868a-0a8a50ebdea5"),
                requiresRowValidation = true,
            });
        }

        public static JObject ValidCheckSplitterValidationEventCreateRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.CheckSplitter,
                dataCount = 1
            });
        }

        public static JObject ValidProducerValidationEventCreateRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.ProducerValidation,
                producerId = "123456"
            });
        }

        public static JObject ValidRegistrationValidationEventCreateRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.Registration,
                requiresBrandsFile = false,
                requiresPartnershipsFile = false,
                organisationMemberCount = 10
            });
        }

        public static JObject ValidRegistrationValidationEventCreateRequestWithRowErrors()
        {
            return JObject.FromObject(new
            {
                type = EventType.Registration,
                requiresBrandsFile = false,
                requiresPartnershipsFile = false,
                HasMaxRowErrors = true,
                RowErrorCount = 200,
            });
        }

        public static JObject ValidBrandValidationEventCreateRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.BrandValidation,
                errors = new JArray { "802" },
                isValid = false,
                blobName = "brand-registrations.csv",
                blobContainerName = "registration-upload-container",
            });
        }

        public static JObject ValidPartnerValidationEventCreateRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.PartnerValidation,
                errors = new JArray { "802" },
                isValid = false,
                blobName = "brand-registrations.csv",
                blobContainerName = "registration-upload-container",
            });
        }

        public static JObject ValidRegulatorPoMDecisionEventCreateRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.RegulatorPoMDecision,
                decision = RegulatorDecision.Accepted
            });
        }

        public static JObject ValidRegulatorRegistrationDecisionEventCreateRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.RegulatorRegistrationDecision,
                decision = RegulatorDecision.Accepted
            });
        }

        public static JObject ValidRegistrationFeePaymentEventCreateRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.RegistrationFeePayment,
                paymentmethod = "offine",
                paymentstatus = "paid",
                paidamount = "£100.00"
            });
        }

        public static JObject ValidPackagingDataResubmissionFeePaymentCreateRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.PackagingDataResubmissionFeePayment,
                paymentmethod = "offine",
                paymentstatus = "paid",
                paidamount = "£50.00"
            });
        }

        public static JObject ValidFileDownloadCheckEventCreateRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.FileDownloadCheck,
                decision = RegulatorDecision.Accepted
            });
        }

        public static JObject ValidPackagingResubmissionReferenceNumberCreatedRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.PackagingResubmissionReferenceNumberCreated,
                packagingResubmissionReferenceNumber = "test-ref"
            });
        }

        public static JObject ValidPackagingResubmissionFeeViewCreatedRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.PackagingResubmissionFeeViewed,
                PackagingResubmissionFeeViewed = true
            });
        }

        public static JObject ValidPackagingResubmissionSubmittedCreatedRequest()
        {
            return JObject.FromObject(new
            {
                type = EventType.PackagingResubmissionApplicationSubmitted,
                IsResubmitted = true
            });
        }
    }

    public static class ValidationEventError
    {
        public static JObject ValidValidationEventError(ValidationType errorType)
        {
            return errorType switch
            {
                ValidationType.CheckSplitter => ValidCheckSplitterValidationEventError(),
                ValidationType.ProducerValidation => ValidProducerValidationEventError(),
                ValidationType.Registration => ValidRegistrationValidationEventError(),
                _ => throw new NotImplementedException()
            };
        }

        public static JObject ValidValidationEventWarning(ValidationType errorType)
        {
            return errorType switch
            {
                ValidationType.CheckSplitter => ValidCheckSplitterValidationEventWarning(),
                _ => throw new NotImplementedException()
            };
        }

        public static JObject ValidCheckSplitterValidationEventWarning()
        {
            return JObject.FromObject(new
            {
                rowNumber = 1,
                producerType = "PT",
                producerSize = "PS",
                wasteType = "WT",
                datasubmissionPeriod = "2023-P1",
                subsidiaryId = "jjHF47",
                packagingCategory = "PC",
                materialType = "MT",
                materialSubType = "MST",
                fromHomeNation = "FHN",
                toHomeNation = "THN",
                quantityKg = "QKG",
                quantityUnits = "QU",
                TransitionalPackagingUnits = "TR",
                RecyclabilityRating = "DD",
                ErrorCodes = new List<string>
                {
                    "99"
                }
            });
        }

        public static JObject ValidCheckSplitterValidationEventError()
        {
            return JObject.FromObject(new
            {
                rowNumber = 1
            });
        }

        public static JObject ValidProducerValidationEventError()
        {
            return JObject.FromObject(new
            {
                rowNumber = 1,
                producerType = "PT",
                producerSize = "PS",
                wasteType = "WT",
                datasubmissionPeriod = "2023-P1",
                subsidiaryId = "jjHF47",
                packagingCategory = "PC",
                materialType = "MT",
                materialSubType = "MST",
                fromHomeNation = "FHN",
                toHomeNation = "THN",
                quantityKg = "QKG",
                quantityUnits = "QU",
                TransitionalPackagingUnits = "TR",
                RecyclabilityRating = "DD",
                ErrorCodes = new List<string>
                {
                    "99"
                }
            });
        }

        public static JObject ValidRegistrationValidationEventError()
        {
            return JObject.FromObject(new
            {
                rowNumber = 1
            });
        }
    }
}