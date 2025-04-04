﻿namespace EPR.SubmissionMicroservice.API.Filters.Swashbuckle.Examples;

using System.Diagnostics.CodeAnalysis;
using Data.Enums;
using global::Swashbuckle.AspNetCore.Filters;
using Newtonsoft.Json.Linq;

[ExcludeFromCodeCoverage]
public class PostSubmissionEventExample : IMultipleExamplesProvider<JObject>
{
    public IEnumerable<SwaggerExample<JObject>> GetExamples()
    {
        yield return SwaggerExample.Create(
            "Packaging Resubmission ReferenceNumber Event",
            new JObject
            {
                ["type"] = (int)EventType.PackagingResubmissionReferenceNumberCreated,
                ["packagingResubmissionReferenceNumber"] = "Test-packaging-reference-number"
            });

        yield return SwaggerExample.Create(
            "Packaging Resubmission Fee View Event",
            new JObject
            {
                ["type"] = (int)EventType.PackagingResubmissionFeeViewed,
                ["IsPackagingResubmissionFeeViewed"] = true
            });

        yield return SwaggerExample.Create(
            "Packaging Resubmission Payment Method Event",
            new JObject
            {
                ["type"] = (int)EventType.PackagingDataResubmissionFeePayment,
                ["PaymentMethod"] = "PayByPhone",
                ["PaymentStatus"] = "null",
                ["PaidAmount"] = 0
            });

        yield return SwaggerExample.Create(
            "Packaging Resubmission Application Submitted Event",
            new JObject
            {
                ["type"] = (int)EventType.PackagingResubmissionApplicationSubmitted,
                ["IsResubmitted"] = true
            });

        yield return SwaggerExample.Create(
            "Regulator Organisation Registration Decision Event",
            new JObject
            {
                ["type"] = (int)EventType.RegulatorRegistrationDecision,
                ["decision"] = (int)RegulatorDecision.Cancelled,
                ["comments"] = "An example test comment",
                ["userid"] = "1c1c1be4-76b7-49c6-a23f-c1ce8ffdcde7"
            });

        yield return SwaggerExample.Create(
            "Registration Application Submitted Event",
            new JObject
            {
                ["type"] = (int)EventType.RegistrationApplicationSubmitted,
                ["comments"] = "We have agreed to settle for a part-payment of £24,500",
                ["applicationReferenceNumber"] = "PEPR00002125P1",
                ["submissionDate"] = DateTime.Today,
            });

        yield return SwaggerExample.Create(
            "Anti Virus Check for file upload - PoM",
            new JObject
            {
                ["type"] = (int)EventType.AntivirusCheck,
                ["errors"] = new JArray { "99" },
                ["fileName"] = "PomFileName.csv",
                ["fileType"] = (int)FileType.Pom,
                ["fileId"] = Guid.NewGuid()
            });

        yield return SwaggerExample.Create(
            "Anti Virus Check for file upload - Company Details",
            new JObject
            {
                ["type"] = (int)EventType.AntivirusCheck,
                ["errors"] = new JArray { "99" },
                ["fileName"] = "CompanyDetailsFileName.csv",
                ["fileType"] = (int)FileType.CompanyDetails,
                ["fileId"] = Guid.NewGuid()
            });

        yield return SwaggerExample.Create(
            "Anti Virus Check for file upload - Brands",
            new JObject
            {
                ["type"] = (int)EventType.AntivirusCheck,
                ["errors"] = new JArray { "99" },
                ["fileName"] = "BrandsFileName.csv",
                ["fileType"] = (int)FileType.Brands,
                ["fileId"] = Guid.NewGuid()
            });

        yield return SwaggerExample.Create(
            "Anti Virus Check for file upload - Partnerships",
            new JObject
            {
                ["type"] = (int)EventType.AntivirusCheck,
                ["errors"] = new JArray { "99" },
                ["fileName"] = "PartnershipsFileName.csv",
                ["fileType"] = (int)FileType.Partnerships,
                ["fileId"] = Guid.NewGuid()
            });

        yield return SwaggerExample.Create(
            "Anti Virus Check for file upload - Subsidiary",
            new JObject
            {
                ["type"] = (int)EventType.AntivirusCheck,
                ["errors"] = new JArray { "99" },
                ["fileName"] = "Subsidiary.csv",
                ["fileType"] = (int)FileType.Subsidiaries,
                ["fileId"] = Guid.NewGuid()
            });

        yield return SwaggerExample.Create(
            "Anti Virus Check for file upload - Companies House",
            new JObject
            {
                ["type"] = (int)EventType.AntivirusCheck,
                ["errors"] = new JArray { "99" },
                ["fileName"] = "CompaniesHouseFileName.csv",
                ["fileType"] = (int)FileType.CompaniesHouse,
                ["fileId"] = Guid.NewGuid()
            });

        yield return SwaggerExample.Create(
            "Anti Virus Result",
            new JObject
            {
                ["type"] = (int)EventType.AntivirusResult,
                ["errors"] = new JArray { "99" },
                ["requiresRowValidation"] = true,
                ["antivirusScanResult"] = (int)AntivirusScanResult.Success,
                ["fileId"] = Guid.NewGuid(),
                ["blobName"] = "file-name.csv",
                ["blobContainerName"] = "registration-upload-container"
            });

        yield return SwaggerExample.Create(
            "Validation - Check Splitter",
            new JObject
            {
                ["type"] = (int)EventType.CheckSplitter,
                ["errors"] = new JArray { "99" },
                ["dataCount"] = 2
            });

        yield return SwaggerExample.Create(
            "Validation - Producer",
            new JObject
            {
                ["type"] = (int)EventType.ProducerValidation,
                ["errors"] = new JArray { "99" },
                ["validationErrors"] = new JArray
                {
                    new JObject
                    {
                        ["rowNumber"] = 1,
                        ["producerId"] = "123456",
                        ["subsidiaryId"] = "jjHF47",
                        ["producerSize"] = "S",
                        ["dataSubmissionPeriod"] = "2023-P1",
                        ["producerType"] = "BO",
                        ["wasteType"] = "DC",
                        ["packagingCategory"] = "P1",
                        ["materialType"] = "PL",
                        ["materialSubType"] = "MST",
                        ["fromHomeNation"] = null,
                        ["toHomeNation"] = null,
                        ["quantityKg"] = "1",
                        ["quantityUnits"] = "1",
                        ["errorCodes"] = new JArray { "28" }
                    }
                },
                ["producerId"] = "123456",
            });

        yield return SwaggerExample.Create(
            "Validation - Registration",
            new JObject
            {
                ["type"] = (int)EventType.Registration,
                ["errors"] = new JArray { "99" },
                ["validationErrors"] = new JArray
                {
                    new JObject
                    {
                        ["rowNumber"] = 1,
                        ["organisationId"] = "28",
                        ["subsidiaryId"] = "1",
                        ["columnErrors"] = new JArray
                        {
                            new JObject
                            {
                                ["errorCode"] = "801",
                                ["columnIndex"] = 0,
                                ["columnName"] = "organisation_id",
                            }
                        }
                    }
                },
                ["requiresBrandsFile"] = true,
                ["requiresPartnershipsFile"] = true,
                ["hasMaxRowErrors"] = true,
                ["rowErrorCount"] = 200,
                ["OrganisationMemberCount"] = 10,
                ["isValid"] = true,
                ["blobName"] = "organisation-registrations.csv",
                ["blobContainerName"] = "registration-upload-container"
            });

        yield return SwaggerExample.Create(
            "Validation - Brands",
            new JObject
            {
                ["type"] = (int)EventType.BrandValidation,
                ["errors"] = new JArray { "802" },
                ["isValid"] = false,
                ["blobName"] = "brand-registrations.csv",
                ["blobContainerName"] = "registration-upload-container"
            });

        yield return SwaggerExample.Create(
            "Validation - Partners",
            new JObject
            {
                ["type"] = (int)EventType.PartnerValidation,
                ["errors"] = new JArray { "802" },
                ["isValid"] = false,
                ["blobName"] = "partner-registrations.csv",
                ["blobContainerName"] = "registration-upload-container"
            });
    }
}