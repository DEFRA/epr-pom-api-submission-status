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

        var errorsCount = 0;

        var latestCompanyDetailsAntivirusCheckEvent = submissionEvents.OfType<AntivirusCheckEvent>()
            .Where(x => x.FileType == FileType.CompanyDetails)
            .MaxBy(x => x.Created);

        var submittedRegistrationSetId = latestCompanyDetailsAntivirusCheckEvent?.RegistrationSetId;

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
        }

        var validationPass = isCompanyDetailsFileValid
                             && isBrandsFileValid
                             && isPartnersFileValid
                             && errorsCount == 0;

        var latestCompanyDetailsCreatedDatetime = validationPass ? latestCompanyDetailsAntivirusCheckEvent?.Created : null;
        var latestSubmittedEventCreatedDatetime = submittedEvent?.Created;
        var isLatestSubmittedEventAfterFileUpload = latestSubmittedEventCreatedDatetime > latestCompanyDetailsCreatedDatetime;

        var registrationApplicationSubmittedEvent = registrationApplicationSubmittedEvents.MaxBy(x => x.Created);

        var firstApplicationSubmittedEvent = registrationApplicationSubmittedEvents.OrderBy(x => x.Created).FirstOrDefault();

        bool isLateFeeApplicable;

        if (firstApplicationSubmittedEvent is not null)
        {
            isLateFeeApplicable = firstApplicationSubmittedEvent.SubmissionDate > request.LateFeeDeadline;
        }
        else
        {
            isLateFeeApplicable = DateTime.Today > request.LateFeeDeadline;
        }

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
            IsLateFeeApplicable = isLateFeeApplicable,
            RegistrationApplicationSubmittedDate = registrationApplicationSubmittedEvent?.SubmissionDate,
            RegistrationApplicationSubmittedComment = registrationApplicationSubmittedEvent?.Comments,
            RegistrationReferenceNumber = regulatorRegistrationDecisionEvent?.RegistrationReferenceNumber
        };

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
                "Queried" => ApplicationStatusType.QueriedByRegulator
            };
        }

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

        return response;
    }

    private static async Task<Submission?> GetSubmission(IQueryRepository<Submission> submissionQueryRepository, GetRegistrationApplicationDetailsQuery request, CancellationToken cancellationToken)
    {
        var query = submissionQueryRepository
                    .GetAll(x => x.OrganisationId == request.OrganisationId &&
                            x.SubmissionType == SubmissionType.Registration &&
                            x.SubmissionPeriod == request.SubmissionPeriod)
                    .Where(x => x.ComplianceSchemeId == null || x.ComplianceSchemeId == request.ComplianceSchemeId);

        var submissions = await query.OrderByDescending(x => x.Created).ToListAsync(cancellationToken);
        return submissions.FirstOrDefault();
    }
}