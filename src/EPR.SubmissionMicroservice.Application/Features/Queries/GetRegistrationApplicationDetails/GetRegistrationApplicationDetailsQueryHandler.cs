using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static EPR.SubmissionMicroservice.Application.Features.Queries.Common.GetRegistrationApplicationDetailsResponse;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;

public class GetRegistrationApplicationDetailsQueryHandler(
    IQueryRepository<Submission> submissionQueryRepository,
    IQueryRepository<AbstractSubmissionEvent> submissionEventQueryRepository)
    : IRequestHandler<GetRegistrationApplicationDetailsQuery, ErrorOr<GetRegistrationApplicationDetailsResponse>>
{
    public async Task<ErrorOr<GetRegistrationApplicationDetailsResponse>> Handle(GetRegistrationApplicationDetailsQuery request, CancellationToken cancellationToken)
    {
        var submission = await GetSubmission(submissionQueryRepository, request, cancellationToken);

        if (submission is null)
        {
            return default;
        }

        var submissionEvents = await submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submission.Id)
            .ToListAsync(cancellationToken);

        var submittedEvent = submissionEvents.OfType<SubmittedEvent>()
            .MaxBy(d => d.Created);

        var regulatorRegistrationDecisionEvent = submissionEvents.OfType<RegulatorRegistrationDecisionEvent>()
            .MaxBy(d => d.Created);

        var registrationFeePaymentEvent = submissionEvents.OfType<RegistrationFeePaymentEvent>()
            .Where(s => !string.IsNullOrWhiteSpace(s.ApplicationReferenceNumber))
            .MaxBy(d => d.Created);

        var registrationApplicationSubmittedEvents = submissionEvents.OfType<RegistrationApplicationSubmittedEvent>()
            .Where(s => !string.IsNullOrWhiteSpace(s.ApplicationReferenceNumber))
            .ToList();

        var latestCompanyDetailsAntivirusCheckEvent = submissionEvents.OfType<AntivirusCheckEvent>()
            .Where(x => x.FileType == FileType.CompanyDetails)
            .MaxBy(x => x.Created);

        var submittedRegistrationSetId = latestCompanyDetailsAntivirusCheckEvent?.RegistrationSetId;

        var validationPass = ValidateFiles(latestCompanyDetailsAntivirusCheckEvent, submissionEvents, submittedRegistrationSetId);

        var latestCompanyDetailsCreatedDatetime = validationPass ? latestCompanyDetailsAntivirusCheckEvent?.Created : null;
        var latestSubmittedEventCreatedDatetime = submittedEvent?.Created;
        var isLatestSubmittedEventAfterFileUpload = latestSubmittedEventCreatedDatetime > latestCompanyDetailsCreatedDatetime;

        var registrationApplicationSubmittedEvent = registrationApplicationSubmittedEvents.MaxBy(x => x.Created);

        var firstApplicationSubmittedEvent = registrationApplicationSubmittedEvents.OrderBy(x => x.Created).FirstOrDefault();

        var response = new GetRegistrationApplicationDetailsResponse
        {
            SubmissionId = submission.Id,
            IsSubmitted = submission.IsSubmitted ?? false,
            IsResubmission = submission.IsResubmission,
            ApplicationReferenceNumber = submission.AppReferenceNumber,
            RegistrationFeePaymentMethod = registrationFeePaymentEvent?.PaymentMethod,
            LastSubmittedFile = isLatestSubmittedEventAfterFileUpload
                ? new LastSubmittedFileDetails
                {
                    SubmittedDateTime = submittedEvent?.Created,
                    FileId = submittedEvent?.FileId,
                    SubmittedByName = submittedEvent?.SubmittedBy
                }
                : null,
            RegistrationApplicationSubmittedDate = registrationApplicationSubmittedEvent?.SubmissionDate,
            RegistrationApplicationSubmittedComment = registrationApplicationSubmittedEvent?.Comments,
            RegistrationReferenceNumber = regulatorRegistrationDecisionEvent?.RegistrationReferenceNumber
        };

        IsLateFeeApplicable(response, request, firstApplicationSubmittedEvent, submissionEvents, isLatestSubmittedEventAfterFileUpload, latestSubmittedEventCreatedDatetime);

        SetApplicationStatus(response, isLatestSubmittedEventAfterFileUpload, latestCompanyDetailsCreatedDatetime, regulatorRegistrationDecisionEvent);

        SetResubmissionStatus(response, regulatorRegistrationDecisionEvent, latestCompanyDetailsCreatedDatetime, isLatestSubmittedEventAfterFileUpload, registrationFeePaymentEvent, registrationApplicationSubmittedEvent);

        SetQueryCancelRejectStatus(response, regulatorRegistrationDecisionEvent, latestCompanyDetailsCreatedDatetime, isLatestSubmittedEventAfterFileUpload, registrationFeePaymentEvent, registrationApplicationSubmittedEvent);

        return response;
    }

    private static bool ValidateFiles(AntivirusCheckEvent? latestCompanyDetailsAntivirusCheckEvent, List<AbstractSubmissionEvent> submissionEvents, Guid? submittedRegistrationSetId)
    {
        var errorsCount = 0;

        var isCompanyDetailsFileValid = false;

        var isBrandsFileValid = true;
        var isPartnersFileValid = true;

        if (latestCompanyDetailsAntivirusCheckEvent is not null)
        {
            var latestCompanyDetailsAntivirusResultEvent = submissionEvents.OfType<AntivirusResultEvent>()
                .Where(x => x.FileId == latestCompanyDetailsAntivirusCheckEvent.FileId)
                .MaxBy(x => x.Created);

            var requiresBrandsFile = false;
            var requiresPartnershipsFile = false;

            if (latestCompanyDetailsAntivirusResultEvent is not null)
            {
                var latestRegistrationValidationEvent = submissionEvents.OfType<RegistrationValidationEvent>()
                    .Where(x => x.BlobName == latestCompanyDetailsAntivirusResultEvent.BlobName)
                    .MaxBy(x => x.Created);

                errorsCount += latestRegistrationValidationEvent?.ErrorCount ?? 0;
                requiresBrandsFile = latestRegistrationValidationEvent?.RequiresBrandsFile ?? false;
                requiresPartnershipsFile = latestRegistrationValidationEvent?.RequiresPartnershipsFile ?? false;
                isCompanyDetailsFileValid = latestRegistrationValidationEvent?.IsValid ?? false;
            }

            isBrandsFileValid = IsBrandsFileValid(requiresBrandsFile, isBrandsFileValid, submissionEvents, submittedRegistrationSetId, ref errorsCount);

            isPartnersFileValid = IsPartnersFileValid(requiresPartnershipsFile, isPartnersFileValid, submissionEvents, submittedRegistrationSetId, ref errorsCount);
        }

        var validationPass = isCompanyDetailsFileValid
                             && isBrandsFileValid
                             && isPartnersFileValid
                             && errorsCount == 0;
        return validationPass;
    }

    private static void SetApplicationStatus(GetRegistrationApplicationDetailsResponse response, bool isLatestSubmittedEventAfterFileUpload, DateTime? latestCompanyDetailsCreatedDatetime, RegulatorRegistrationDecisionEvent? regulatorRegistrationDecisionEvent)
    {
        if (response.IsSubmitted)
        {
            response.ApplicationStatus = isLatestSubmittedEventAfterFileUpload
                ? ApplicationStatusType.SubmittedToRegulator
                : ApplicationStatusType.SubmittedAndHasRecentFileUpload;
        }
        else
        {
            response.ApplicationStatus = latestCompanyDetailsCreatedDatetime != null
                ? ApplicationStatusType.FileUploaded
                : ApplicationStatusType.NotStarted;
        }

        if (regulatorRegistrationDecisionEvent is not null)
        {
            response.ApplicationStatus = regulatorRegistrationDecisionEvent.Decision.ToString() switch
            {
                "Accepted" => ApplicationStatusType.AcceptedByRegulator,
                "Approved" => ApplicationStatusType.ApprovedByRegulator,
                "Rejected" => ApplicationStatusType.RejectedByRegulator,
                "Cancelled" => ApplicationStatusType.CancelledByRegulator,
                "Queried" => ApplicationStatusType.QueriedByRegulator,
                _ => throw new InvalidOperationException("Regulator Status Not supported")
            };
        }
    }

    private static void IsLateFeeApplicable(GetRegistrationApplicationDetailsResponse response, GetRegistrationApplicationDetailsQuery request, RegistrationApplicationSubmittedEvent? firstApplicationSubmittedEvent, List<AbstractSubmissionEvent> submissionEvents, bool isLatestSubmittedEventAfterFileUpload, DateTime? latestSubmittedEventCreatedDatetime)
    {
        response.IsLateFeeApplicable = false;
        response.IsOriginalCsoSubmissionLate = false;

        //CSO logic
        if (request.ComplianceSchemeId is not null)
        {
            var hasAnyApprovedRegulatorDecision = submissionEvents
                .OfType<RegulatorRegistrationDecisionEvent>()
                .Any(d => d.Decision is RegulatorDecision.Accepted or RegulatorDecision.Approved);

            if (hasAnyApprovedRegulatorDecision && isLatestSubmittedEventAfterFileUpload)
            {
                response.IsLateFeeApplicable = latestSubmittedEventCreatedDatetime.Value.Date > request.LateFeeDeadline;
            }
            else if (firstApplicationSubmittedEvent is not null)
            {
                response.IsLateFeeApplicable = firstApplicationSubmittedEvent.Created.Date > request.LateFeeDeadline;
            }
            else
            {
                response.IsLateFeeApplicable = DateTime.Today > request.LateFeeDeadline;
            }

            if (firstApplicationSubmittedEvent is not null)
            {
                response.IsOriginalCsoSubmissionLate = firstApplicationSubmittedEvent.Created.Date > request.LateFeeDeadline;
            }
        }
        else
        {
            //Producer Logic
            if (firstApplicationSubmittedEvent is not null)
            {
                response.IsLateFeeApplicable = firstApplicationSubmittedEvent.Created.Date > request.LateFeeDeadline;
            }
            else
            {
                response.IsLateFeeApplicable = DateTime.Today > request.LateFeeDeadline;
            }
        }
    }

    private static void SetQueryCancelRejectStatus(GetRegistrationApplicationDetailsResponse response, RegulatorRegistrationDecisionEvent? regulatorRegistrationDecisionEvent, DateTime? latestCompanyDetailsCreatedDatetime, bool isLatestSubmittedEventAfterFileUpload, RegistrationFeePaymentEvent? registrationFeePaymentEvent, RegistrationApplicationSubmittedEvent? registrationApplicationSubmittedEvent)
    {
        if (response.ApplicationStatus is
                ApplicationStatusType.CancelledByRegulator
                or ApplicationStatusType.QueriedByRegulator
                or ApplicationStatusType.RejectedByRegulator
            && regulatorRegistrationDecisionEvent.Created < latestCompanyDetailsCreatedDatetime)
        {
            response.ApplicationStatus = isLatestSubmittedEventAfterFileUpload
                ? ApplicationStatusType.SubmittedToRegulator
                : ApplicationStatusType.SubmittedAndHasRecentFileUpload;

            if (registrationFeePaymentEvent?.Created < regulatorRegistrationDecisionEvent.Created)
            {
                response.RegistrationFeePaymentMethod = null;
            }

            if (registrationApplicationSubmittedEvent?.Created < regulatorRegistrationDecisionEvent.Created)
            {
                response.RegistrationApplicationSubmittedComment = null;
                response.RegistrationApplicationSubmittedDate = null;
            }
        }
    }

    private static void SetResubmissionStatus(GetRegistrationApplicationDetailsResponse response, RegulatorRegistrationDecisionEvent? regulatorRegistrationDecisionEvent, DateTime? latestCompanyDetailsCreatedDatetime, bool isLatestSubmittedEventAfterFileUpload, RegistrationFeePaymentEvent? registrationFeePaymentEvent, RegistrationApplicationSubmittedEvent? registrationApplicationSubmittedEvent)
    {
        if (response.ApplicationStatus is
                ApplicationStatusType.ApprovedByRegulator
                or ApplicationStatusType.AcceptedByRegulator
            && regulatorRegistrationDecisionEvent.Created < latestCompanyDetailsCreatedDatetime)
        {
            response.ApplicationStatus = isLatestSubmittedEventAfterFileUpload
                ? ApplicationStatusType.SubmittedToRegulator
                : ApplicationStatusType.SubmittedAndHasRecentFileUpload;

            response.IsResubmission = true;

            if (registrationFeePaymentEvent?.Created < regulatorRegistrationDecisionEvent.Created)
            {
                response.RegistrationFeePaymentMethod = null;
            }

            if (registrationApplicationSubmittedEvent?.Created < regulatorRegistrationDecisionEvent.Created)
            {
                response.RegistrationApplicationSubmittedComment = null;
                response.RegistrationApplicationSubmittedDate = null;
            }
        }
    }

    private static bool IsPartnersFileValid(bool requiresPartnershipsFile, bool isPartnersFileValid, List<AbstractSubmissionEvent> submissionEvents, Guid? submittedRegistrationSetId, ref int errorsCount)
    {
        if (requiresPartnershipsFile)
        {
            isPartnersFileValid = false;
            var latestPartnershipsAntivirusCheckEvent = submissionEvents.OfType<AntivirusCheckEvent>()
                .Where(x => x.FileType == FileType.Partnerships && (submittedRegistrationSetId is null || x.RegistrationSetId == submittedRegistrationSetId))
                .MaxBy(x => x.Created);

            if (latestPartnershipsAntivirusCheckEvent is not null)
            {
                var latestPartnershipsAntivirusResultEvent = submissionEvents.OfType<AntivirusResultEvent>()
                    .Where(x => x.FileId == latestPartnershipsAntivirusCheckEvent.FileId)
                    .MaxBy(x => x.Created);

                if (latestPartnershipsAntivirusResultEvent is not null)
                {
                    var latestPartnerValidationEvent = submissionEvents.OfType<PartnerValidationEvent>()
                        .Where(x => x.BlobName == latestPartnershipsAntivirusResultEvent.BlobName)
                        .MaxBy(x => x.Created);

                    isPartnersFileValid = latestPartnerValidationEvent?.IsValid == true;
                    errorsCount += latestPartnerValidationEvent?.ErrorCount ?? 0;
                }
            }
        }

        return isPartnersFileValid;
    }

    private static bool IsBrandsFileValid(bool requiresBrandsFile, bool isBrandsFileValid, List<AbstractSubmissionEvent> submissionEvents, Guid? submittedRegistrationSetId, ref int errorsCount)
    {
        if (requiresBrandsFile)
        {
            isBrandsFileValid = false;
            var latestBrandsAntivirusCheckEvent = submissionEvents.OfType<AntivirusCheckEvent>()
                .Where(x => x.FileType == FileType.Brands && (submittedRegistrationSetId is null || x.RegistrationSetId == submittedRegistrationSetId))
                .MaxBy(x => x.Created);

            if (latestBrandsAntivirusCheckEvent is not null)
            {
                var latestBrandsAntivirusResultEvent = submissionEvents.OfType<AntivirusResultEvent>()
                    .Where(x => x.FileId == latestBrandsAntivirusCheckEvent.FileId)
                    .MaxBy(x => x.Created);

                if (latestBrandsAntivirusResultEvent is not null)
                {
                    var latestBrandValidationEvent = submissionEvents.OfType<BrandValidationEvent>()
                        .Where(x => x.BlobName == latestBrandsAntivirusResultEvent.BlobName)
                        .MaxBy(x => x.Created);

                    isBrandsFileValid = latestBrandValidationEvent?.IsValid == true;
                    errorsCount += latestBrandValidationEvent?.ErrorCount ?? 0;
                }
            }
        }

        return isBrandsFileValid;
    }

    private static async Task<Submission?> GetSubmission(IQueryRepository<Submission> submissionQueryRepository, GetRegistrationApplicationDetailsQuery request, CancellationToken cancellationToken)
    {
        var query = submissionQueryRepository
            .GetAll(x => x.OrganisationId == request.OrganisationId &&
                         x.SubmissionType == SubmissionType.Registration &&
                         x.SubmissionPeriod == request.SubmissionPeriod)
            .Where(x => x.ComplianceSchemeId == null || x.ComplianceSchemeId == request.ComplianceSchemeId);

        var submission = await query.OrderByDescending(x => x.Created).FirstOrDefaultAsync(cancellationToken);

        return submission;
    }
}