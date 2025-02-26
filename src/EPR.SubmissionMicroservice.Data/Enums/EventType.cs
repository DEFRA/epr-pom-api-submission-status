namespace EPR.SubmissionMicroservice.Data.Enums;

public enum EventType
{
    AntivirusCheck = 1,
    CheckSplitter = 2,
    ProducerValidation = 3,
    Registration = 4,
    AntivirusResult = 5,
    Submitted = 6,
    RegulatorPoMDecision = 7,
    BrandValidation = 8,
    PartnerValidation = 9,
    RegulatorRegistrationDecision = 10,
    FileDownloadCheck = 11,
    RegistrationFeePayment = 12,
    RegistrationApplicationSubmitted = 13,
    PackagingDataResubmissionFeePayment = 14,
    SubsidiariesBulkUploadComplete = 18
}