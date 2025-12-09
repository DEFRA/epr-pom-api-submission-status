using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Options;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static EPR.SubmissionMicroservice.Application.Features.Queries.Common.GetRegistrationApplicationDetailsResponse;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;

public class GetRegistrationApplicationDetailsQueryHandler(
    IQueryRepository<Submission> submissionQueryRepository,
    IQueryRepository<AbstractSubmissionEvent> submissionEventQueryRepository,
    IOptions<FeatureFlagOptions> featureFlagOptions)
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

        var regulatorRegistrationDecisionEvents = submissionEvents.OfType<RegulatorRegistrationDecisionEvent>().OrderBy(x => x.Created).ToList();
        var regulatorRegistrationDecisionEvent = regulatorRegistrationDecisionEvents.MaxBy(d => d.Created);

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
            //always return LastSubmittedFileDetails based on the latest submittedEvent then check on the frontEnd if the file has reached synapse or not
            LastSubmittedFile = new LastSubmittedFileDetails
            {
                SubmittedDateTime = submittedEvent?.Created,
                FileId = submittedEvent?.FileId,
                SubmittedByName = submittedEvent?.SubmittedBy
            },
            RegistrationApplicationSubmittedDate = registrationApplicationSubmittedEvent?.SubmissionDate,
            RegistrationApplicationSubmittedComment = registrationApplicationSubmittedEvent?.Comments,
            RegistrationReferenceNumber = regulatorRegistrationDecisionEvents.Find(x => !string.IsNullOrWhiteSpace(x.RegistrationReferenceNumber) && x.Decision is RegulatorDecision.Accepted or RegulatorDecision.Approved)?.RegistrationReferenceNumber,
            HasAnyApprovedOrQueriedRegulatorDecision = submissionEvents
                .OfType<RegulatorRegistrationDecisionEvent>()
                .Any(d => d.Decision is RegulatorDecision.Accepted or RegulatorDecision.Approved || (featureFlagOptions.Value.IsQueryLateFeeEnabled && submission.SubmissionPeriod.Contains("2026") && d.Decision == RegulatorDecision.Queried)),
            IsLatestSubmittedEventAfterFileUpload = isLatestSubmittedEventAfterFileUpload,
            LatestSubmittedEventCreatedDatetime = latestSubmittedEventCreatedDatetime,
            FirstApplicationSubmittedEventCreatedDatetime = firstApplicationSubmittedEvent?.Created.Date,
            RegistrationJourney = submission.RegistrationJourney,
        };

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
                    x.SubmissionPeriod == request.SubmissionPeriod);

        if (!string.IsNullOrWhiteSpace(request.RegistrationJourney))
        {
            query.Where(x => x.RegistrationJourney == request.RegistrationJourney);
        }

        if (request.ComplianceSchemeId is not null)
        {
            query = query.Where(x => x.ComplianceSchemeId == request.ComplianceSchemeId);
        }

        var submissions = await query.OrderByDescending(x => x.Created).ToListAsync(cancellationToken);
        return submissions.FirstOrDefault();
    }
}