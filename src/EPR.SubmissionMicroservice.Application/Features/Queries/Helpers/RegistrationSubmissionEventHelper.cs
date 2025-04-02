namespace EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;

using Common;
using Data.Entities.AntivirusEvents;
using Data.Entities.SubmissionEvent;
using Data.Enums;
using Data.Repositories.Queries.Interfaces;
using Interfaces;
using Microsoft.EntityFrameworkCore;

public class RegistrationSubmissionEventHelper : IRegistrationSubmissionEventHelper
{
    private readonly IQueryRepository<AbstractSubmissionEvent> _submissionEventQueryRepository;

    public RegistrationSubmissionEventHelper(IQueryRepository<AbstractSubmissionEvent> submissionEventQueryRepository)
    {
        _submissionEventQueryRepository = submissionEventQueryRepository;
    }

    public static bool IsEventValid(bool? requiresRowValidation, AbstractValidationEvent? validationEvent)
    {
        return requiresRowValidation != true || validationEvent?.IsValid == true;
    }

    public static bool IsBrandsFileValid(RegistrationSubmissionGetResponse response)
    {
        return !response.RequiresBrandsFile || (response.BrandsDataComplete && response.BrandsDataIsValid);
    }

    public static bool IsPartnersFileValid(RegistrationSubmissionGetResponse response)
    {
        return !response.RequiresPartnershipsFile || (response.PartnershipsDataComplete && response.PartnersDataIsValid);
    }

    public async Task SetValidationEvents(RegistrationSubmissionGetResponse response, bool isSubmitted, CancellationToken cancellationToken)
    {
        var submissionId = response.Id;
        var events = await GetEventsAsync(submissionId, cancellationToken);
        var antivirusCheckEvents = events.OfType<AntivirusCheckEvent>().ToList();

        if (antivirusCheckEvents.Count == 0)
        {
            return;
        }

        var antivirusResultEvents = events.OfType<AntivirusResultEvent>().ToList();
        var registrationValidationEvents = events.OfType<RegistrationValidationEvent>().ToList();
        var submittedEvents = events.OfType<SubmittedEvent>().ToList();
        var latestCompanyDetailsAntivirusCheckEvent = GetLatestAntivirusCheckEventByFileType(antivirusCheckEvents, FileType.CompanyDetails);
        var latestCompanyDetailsAntivirusResultEvent = GetAntivirusResultEventByFileId(antivirusResultEvents, latestCompanyDetailsAntivirusCheckEvent!.FileId);
        var latestFileUploadErrors = new List<string>();
        var companyDetailsDataComplete = false;
        var requiresBrandsFile = false;
        var requiresPartnershipsFile = false;
        var registrationSetId = latestCompanyDetailsAntivirusCheckEvent.RegistrationSetId;

        if (latestCompanyDetailsAntivirusResultEvent is not null)
        {
            AddErrorsToListIfPresent(latestFileUploadErrors, latestCompanyDetailsAntivirusResultEvent.Errors);

            var latestRegistrationValidationEvent = GetRegistrationValidationEventByBlobName(registrationValidationEvents, latestCompanyDetailsAntivirusResultEvent.BlobName);

            if (latestRegistrationValidationEvent is not null)
            {
                requiresBrandsFile = latestRegistrationValidationEvent.RequiresBrandsFile;
                requiresPartnershipsFile = latestRegistrationValidationEvent.RequiresPartnershipsFile;
                companyDetailsDataComplete = true;
                response.OrganisationMemberCount = latestRegistrationValidationEvent.OrganisationMemberCount;
                AddErrorsToListIfPresent(latestFileUploadErrors, latestRegistrationValidationEvent.Errors);
                response.HasMaxRowErrors = latestRegistrationValidationEvent.HasMaxRowErrors.GetValueOrDefault();
                response.RowErrorCount = latestRegistrationValidationEvent.RowErrorCount.GetValueOrDefault(0);
                response.CompanyDetailsFileIsValid = latestRegistrationValidationEvent.IsValid == true;
                response.HasWarnings = latestRegistrationValidationEvent.WarningCount > 0;
            }
        }

        response.CompanyDetailsDataComplete = companyDetailsDataComplete;
        response.CompanyDetailsUploadedBy = latestCompanyDetailsAntivirusCheckEvent.UserId;
        response.CompanyDetailsUploadedDate = latestCompanyDetailsAntivirusCheckEvent.Created;
        response.CompanyDetailsFileName = latestCompanyDetailsAntivirusCheckEvent.FileName;

        if (requiresBrandsFile)
        {
            response.RequiresBrandsFile = true;
            var latestBrandsAntivirusCheckEvent = registrationSetId is null
                ? GetLatestAntivirusCheckEventByFileType(antivirusCheckEvents, FileType.Brands)
                : GetAntivirusCheckEventByFileTypeAndRegistrationSetId(antivirusCheckEvents, registrationSetId.Value, FileType.Brands);

            if (latestBrandsAntivirusCheckEvent is not null)
            {
                AddErrorsToListIfPresent(latestFileUploadErrors, latestBrandsAntivirusCheckEvent.Errors);

                response.BrandsFileName = latestBrandsAntivirusCheckEvent.FileName;
                response.BrandsUploadedBy = latestBrandsAntivirusCheckEvent.UserId;
                response.BrandsUploadedDate = latestBrandsAntivirusCheckEvent.Created;

                var latestBrandsAntivirusResultEvent = GetAntivirusResultEventByFileId(antivirusResultEvents, latestBrandsAntivirusCheckEvent.FileId);

                if (latestBrandsAntivirusResultEvent is not null)
                {
                    AddErrorsToListIfPresent(latestFileUploadErrors, latestBrandsAntivirusResultEvent.Errors);
                    response.BrandsDataComplete = latestBrandsAntivirusResultEvent is { AntivirusScanResult: AntivirusScanResult.Success };
                    response.BrandsDataIsValid = response.BrandsDataComplete;

                    if (latestBrandsAntivirusResultEvent.RequiresRowValidation == true)
                    {
                        var latestBrandValidationEvent = GetLatestBrandValidationEventByBlobName(events, latestBrandsAntivirusResultEvent);
                        response.BrandsDataComplete = latestBrandValidationEvent != null;
                        response.BrandsDataIsValid = latestBrandValidationEvent?.IsValid == true;
                        AddErrorsToListIfPresent(latestFileUploadErrors, latestBrandValidationEvent?.Errors);
                    }
                }
            }
        }

        if (requiresPartnershipsFile)
        {
            response.RequiresPartnershipsFile = true;
            var latestPartnershipsAntivirusCheckEvent = registrationSetId is null
                ? GetLatestAntivirusCheckEventByFileType(antivirusCheckEvents, FileType.Partnerships)
                : GetAntivirusCheckEventByFileTypeAndRegistrationSetId(antivirusCheckEvents, registrationSetId.Value, FileType.Partnerships);

            if (latestPartnershipsAntivirusCheckEvent is not null)
            {
                AddErrorsToListIfPresent(latestFileUploadErrors, latestPartnershipsAntivirusCheckEvent.Errors);

                response.PartnershipsFileName = latestPartnershipsAntivirusCheckEvent.FileName;
                response.PartnershipsUploadedBy = latestPartnershipsAntivirusCheckEvent.UserId;
                response.PartnershipsUploadedDate = latestPartnershipsAntivirusCheckEvent.Created;

                var latestPartnershipsAntivirusResultEvent = GetAntivirusResultEventByFileId(antivirusResultEvents, latestPartnershipsAntivirusCheckEvent.FileId);

                if (latestPartnershipsAntivirusResultEvent is not null)
                {
                    AddErrorsToListIfPresent(latestFileUploadErrors, latestPartnershipsAntivirusResultEvent.Errors);
                    response.PartnershipsDataComplete = latestPartnershipsAntivirusResultEvent is { AntivirusScanResult: AntivirusScanResult.Success };
                    response.PartnersDataIsValid = response.PartnershipsDataComplete;

                    if (latestPartnershipsAntivirusResultEvent.RequiresRowValidation == true)
                    {
                        var latestPartnerValidationEvent = GetLatestPartnerValidationEventByBlobName(events, latestPartnershipsAntivirusResultEvent);
                        response.BrandsDataComplete = latestPartnerValidationEvent != null;
                        response.PartnersDataIsValid = latestPartnerValidationEvent?.IsValid == true;
                        AddErrorsToListIfPresent(latestFileUploadErrors, latestPartnerValidationEvent?.Errors);
                    }
                }
            }
        }

        if (isSubmitted)
        {
            var latestSubmittedEvent = submittedEvents.MaxBy(x => x.Created);
            var companyDetailsFileId = latestSubmittedEvent!.FileId;
            var companyDetailsFile = GetAntivirusCheckEventByFileIdAndType(antivirusCheckEvents, companyDetailsFileId, FileType.CompanyDetails);
            var antivirusCheckEvent = GetAntivirusResultEventByFileId(antivirusResultEvents, companyDetailsFileId);
            var registrationEvent = GetRegistrationValidationEventByBlobName(registrationValidationEvents, antivirusCheckEvent!.BlobName);
            var submittedRegistrationSetId = companyDetailsFile!.RegistrationSetId;

            var submittedRegistrationInformation = new SubmittedRegistrationFilesInformation
            {
                CompanyDetailsFileName = companyDetailsFile.FileName,
                SubmittedDateTime = latestSubmittedEvent.Created,
                SubmittedBy = latestSubmittedEvent.UserId,
            };

            if (registrationEvent.RequiresBrandsFile)
            {
                var brandsAntivirusCheckEvent = submittedRegistrationSetId is null
                    ? GetLatestAntivirusCheckEventByFileTypeWhereRegistrationSetIdIsNull(antivirusCheckEvents, FileType.Brands)
                    : GetAntivirusCheckEventByFileTypeAndRegistrationSetId(antivirusCheckEvents, submittedRegistrationSetId.Value, FileType.Brands);
                submittedRegistrationInformation.BrandsFileName = brandsAntivirusCheckEvent.FileName;
            }

            if (registrationEvent.RequiresPartnershipsFile)
            {
                var partnershipsAntivirusCheckEvent = submittedRegistrationSetId is null
                    ? GetLatestAntivirusCheckEventByFileTypeWhereRegistrationSetIdIsNull(antivirusCheckEvents, FileType.Partnerships)
                    : GetAntivirusCheckEventByFileTypeAndRegistrationSetId(antivirusCheckEvents, submittedRegistrationSetId.Value, FileType.Partnerships);
                submittedRegistrationInformation.PartnersFileName = partnershipsAntivirusCheckEvent.FileName;
            }

            response.LastSubmittedFiles = submittedRegistrationInformation;
        }

        response.Errors = latestFileUploadErrors;
        response.ValidationPass = response.CompanyDetailsDataComplete
                                  && response.CompanyDetailsFileIsValid
                                  && IsBrandsFileValid(response)
                                  && IsPartnersFileValid(response)
                                  && response.Errors.Count == 0;

        if (response.ValidationPass)
        {
            response.LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = latestCompanyDetailsAntivirusCheckEvent.FileId,
                CompanyDetailsFileName = response.CompanyDetailsFileName,
                CompanyDetailsUploadDatetime = response.CompanyDetailsUploadedDate.Value,
                CompanyDetailsUploadedBy = response.CompanyDetailsUploadedBy.Value,
                BrandsFileName = response.BrandsFileName,
                BrandsUploadDatetime = response.BrandsUploadedDate,
                BrandsUploadedBy = response.BrandsUploadedBy,
                PartnershipsFileName = response.PartnershipsFileName,
                PartnershipsUploadDatetime = response.PartnershipsUploadedDate,
                PartnershipsUploadedBy = response.PartnershipsUploadedBy
            };

            return;
        }

        var antivirusCheckCompanyDetailsEvents = antivirusCheckEvents
            .Where(x => x.FileId != latestCompanyDetailsAntivirusCheckEvent.FileId && x.FileType == FileType.CompanyDetails)
            .OrderByDescending(x => x.Created)
            .ToList();

        foreach (var companyDetailsEvent in antivirusCheckCompanyDetailsEvents)
        {
            var antivirusResultEvent = GetAntivirusResultEventByFileId(antivirusResultEvents, companyDetailsEvent.FileId);

            if (antivirusResultEvent is not { AntivirusScanResult: AntivirusScanResult.Success })
            {
                continue;
            }

            var registrationEvent = GetRegistrationValidationEventByBlobName(registrationValidationEvents, antivirusResultEvent.BlobName);

            if (registrationEvent is null || registrationEvent.IsValid != true)
            {
                continue;
            }

            AntivirusCheckEvent? brandsAntivirusCheckEvent = null;
            AntivirusCheckEvent? partnershipsAntivirusCheckEvent = null;
            AntivirusResultEvent? brandsAntivirusResultEvent = null;
            AntivirusResultEvent? partnershipsAntivirusResultEvent = null;
            BrandValidationEvent? brandValidationEvent = null;
            PartnerValidationEvent? partnerValidationEvent = null;

            if (registrationEvent is { RequiresBrandsFile: true })
            {
                var brandsAntivirusCheck = companyDetailsEvent.RegistrationSetId is null
                    ? GetLatestAntivirusCheckEventByFileTypeWhereRegistrationSetIdIsNull(antivirusCheckEvents, FileType.Brands)
                    : GetAntivirusCheckEventByFileTypeAndRegistrationSetId(antivirusCheckEvents, companyDetailsEvent.RegistrationSetId.Value, FileType.Brands);

                if (brandsAntivirusCheck is not null)
                {
                    brandsAntivirusResultEvent = GetAntivirusResultEventByFileId(antivirusResultEvents, brandsAntivirusCheck.FileId);

                    if (brandsAntivirusResultEvent is { AntivirusScanResult: AntivirusScanResult.Success })
                    {
                        brandsAntivirusCheckEvent = brandsAntivirusCheck;
                    }

                    if (brandsAntivirusResultEvent?.RequiresRowValidation == true)
                    {
                        brandValidationEvent = events
                            .OfType<BrandValidationEvent>()
                            .Where(x => x.BlobName == brandsAntivirusResultEvent.BlobName)
                            .MaxBy(x => x.Created);
                    }
                }
            }

            if (registrationEvent is { RequiresPartnershipsFile: true })
            {
                var partnershipsAntivirusCheck = companyDetailsEvent.RegistrationSetId is null
                    ? GetLatestAntivirusCheckEventByFileTypeWhereRegistrationSetIdIsNull(antivirusCheckEvents, FileType.Partnerships)
                    : GetAntivirusCheckEventByFileTypeAndRegistrationSetId(antivirusCheckEvents, companyDetailsEvent.RegistrationSetId.Value, FileType.Partnerships);

                if (partnershipsAntivirusCheck is not null)
                {
                    partnershipsAntivirusResultEvent = GetAntivirusResultEventByFileId(antivirusResultEvents, partnershipsAntivirusCheck.FileId);

                    if (partnershipsAntivirusResultEvent is { AntivirusScanResult: AntivirusScanResult.Success })
                    {
                        partnershipsAntivirusCheckEvent = partnershipsAntivirusCheck;
                    }

                    if (partnershipsAntivirusResultEvent?.RequiresRowValidation == true)
                    {
                        partnerValidationEvent = events
                            .OfType<PartnerValidationEvent>()
                            .Where(x => x.BlobName == partnershipsAntivirusResultEvent.BlobName)
                            .MaxBy(x => x.Created);
                    }
                }
            }

            var brandsIsValid = registrationEvent.RequiresBrandsFile
                ? brandsAntivirusCheckEvent is not null && IsEventValid(brandsAntivirusResultEvent.RequiresRowValidation, brandValidationEvent)
                : brandsAntivirusCheckEvent is null;
            var partnershipsIsValid = registrationEvent.RequiresPartnershipsFile
                ? partnershipsAntivirusCheckEvent is not null && IsEventValid(partnershipsAntivirusResultEvent.RequiresRowValidation, partnerValidationEvent)
                : partnershipsAntivirusCheckEvent is null;

            if (brandsIsValid && partnershipsIsValid)
            {
                response.LastUploadedValidFiles = new UploadedRegistrationFilesInformation
                {
                    CompanyDetailsFileId = companyDetailsEvent.FileId,
                    CompanyDetailsFileName = companyDetailsEvent.FileName,
                    CompanyDetailsUploadDatetime = companyDetailsEvent.Created,
                    CompanyDetailsUploadedBy = companyDetailsEvent.UserId,
                    BrandsFileName = brandsAntivirusCheckEvent?.FileName,
                    BrandsUploadDatetime = brandsAntivirusCheckEvent?.Created,
                    BrandsUploadedBy = brandsAntivirusCheckEvent?.UserId,
                    PartnershipsFileName = partnershipsAntivirusCheckEvent?.FileName,
                    PartnershipsUploadDatetime = partnershipsAntivirusCheckEvent?.Created,
                    PartnershipsUploadedBy = partnershipsAntivirusCheckEvent?.UserId
                };

                break;
            }
        }
    }

    private static void AddErrorsToListIfPresent(List<string> errors, List<string>? eventErrors)
    {
        if (eventErrors != null && eventErrors.Count > 0)
        {
            errors.AddRange(eventErrors);
        }
    }

    private static AntivirusCheckEvent? GetLatestAntivirusCheckEventByFileType(
        List<AntivirusCheckEvent> events,
        FileType fileType)
    {
        return events
            .Where(x => x.FileType == fileType)
            .MaxBy(x => x.Created);
    }

    private static AntivirusCheckEvent? GetLatestAntivirusCheckEventByFileTypeWhereRegistrationSetIdIsNull(
        List<AntivirusCheckEvent> events,
        FileType fileType)
    {
        return events
            .Where(x => x.FileType == fileType && x.RegistrationSetId is null)
            .MaxBy(x => x.Created);
    }

    private static AntivirusCheckEvent? GetAntivirusCheckEventByFileIdAndType(
        List<AntivirusCheckEvent> events,
        Guid fileId,
        FileType fileType)
    {
        return events.Find(x => x.FileId == fileId && x.FileType == fileType);
    }

    private static AntivirusResultEvent? GetAntivirusResultEventByFileId(List<AntivirusResultEvent> events, Guid fileId)
    {
        return events
            .Where(x => x.FileId == fileId)
            .MaxBy(x => x.Created);
    }

    private static RegistrationValidationEvent? GetRegistrationValidationEventByBlobName(
        List<RegistrationValidationEvent> events,
        string blobName)
    {
        return events
            .Where(x => x.BlobName == blobName)
            .MaxBy(x => x.Created);
    }

    private static PartnerValidationEvent? GetLatestPartnerValidationEventByBlobName(
        List<AbstractSubmissionEvent> events,
        AntivirusResultEvent latestPartnershipsAntivirusResultEvent)
    {
        return events
            .OfType<PartnerValidationEvent>()
            .Where(x => x.BlobName == latestPartnershipsAntivirusResultEvent.BlobName)
            .MaxBy(x => x.Created);
    }

    private static BrandValidationEvent? GetLatestBrandValidationEventByBlobName(
        List<AbstractSubmissionEvent> events,
        AntivirusResultEvent latestBrandsAntivirusResultEvent)
    {
        return events
            .OfType<BrandValidationEvent>()
            .Where(x => x.BlobName == latestBrandsAntivirusResultEvent.BlobName)
            .MaxBy(x => x.Created);
    }

    private static AntivirusCheckEvent? GetAntivirusCheckEventByFileTypeAndRegistrationSetId(
        List<AntivirusCheckEvent> events,
        Guid registrationSetId,
        FileType fileType)
    {
        return events
            .Where(x => x.FileType == fileType && x.RegistrationSetId == registrationSetId)
            .MaxBy(x => x.Created);
    }

    private async Task<List<AbstractSubmissionEvent>> GetEventsAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        var res = _submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submissionId)
            .ToListAsync(cancellationToken);
        return await res;
    }
}