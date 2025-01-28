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
        var query = submissionQueryRepository
            .GetAll(x =>
                x.OrganisationId == request.OrganisationId &&
                x.SubmissionType == SubmissionType.Registration &&
                x.SubmissionPeriod == request.SubmissionPeriod &&
                x.Created != null);

        if (request.ComplianceSchemeId is not null)
        {
            query = query.Where(x => x.ComplianceSchemeId == request.ComplianceSchemeId);
        }

        var submission = await query.OrderByDescending(x => x.Created).FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
        {
            return default;
        }

        var submissionEvents = await submissionEventQueryRepository
            .GetAll(x => x.SubmissionId == submission.Id)
            .ToListAsync(cancellationToken);

        var submittedEvent = submissionEvents.OfType<SubmittedEvent>()
            .MaxBy(d => d.Created);

        var regulatorRegistrationDecision = submissionEvents.OfType<RegulatorRegistrationDecisionEvent>()
            .MaxBy(d => d.Created);

        var registrationFeePayment = submissionEvents.OfType<RegistrationFeePaymentEvent>()
            .Where(s => !string.IsNullOrWhiteSpace(s.ApplicationReferenceNumber))
            .MaxBy(d => d.Created);

        var registrationApplicationSubmitted = submissionEvents.OfType<RegistrationApplicationSubmittedEvent>()
            .Where(s => !string.IsNullOrWhiteSpace(s.ApplicationReferenceNumber))
            .MaxBy(d => d.Created);

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

        var lastUploadedValidFilesCompanyDetailsUploadDatetime =
            validationPass ? latestCompanyDetailsAntivirusCheckEvent?.Created : null;

        var response = new GetRegistrationApplicationDetailsResponse
        {
            SubmissionId = submission.Id,
            IsSubmitted = submission.IsSubmitted ?? false,
            ApplicationReferenceNumber = submission.AppReferenceNumber,
            RegistrationFeePaymentMethod = registrationFeePayment?.PaymentMethod,
            LastSubmittedFile = new LastSubmittedFileDetails
            {
                SubmittedDateTime = submittedEvent?.Created,
                FileId = submittedEvent?.FileId,
                SubmittedByName = submittedEvent?.SubmittedBy,
            },
            RegistrationApplicationSubmittedDate = registrationApplicationSubmitted?.SubmissionDate,
            RegistrationApplicationSubmittedComment = registrationApplicationSubmitted?.Comments
        };

        if (!response.IsSubmitted)
        {
            response.ApplicationStatus = lastUploadedValidFilesCompanyDetailsUploadDatetime != null
                ? ApplicationStatusType.FileUploaded
                : ApplicationStatusType.NotStarted;
        }
        else
        {
            switch (regulatorRegistrationDecision?.Decision.ToString())
            {
                case "Accepted":
                    response.ApplicationStatus = ApplicationStatusType.AcceptedByRegulator;
                    break;
                case "Approved":
                    response.ApplicationStatus = ApplicationStatusType.ApprovedByRegulator;
                    break;
                case "Rejected":
                    response.ApplicationStatus = ApplicationStatusType.RejectedByRegulator;
                    break;
                case "Cancelled":
                    response.ApplicationStatus = ApplicationStatusType.CancelledByRegulator;
                    break;
                case "Queried":
                    response.ApplicationStatus = ApplicationStatusType.QueriedByRegulator;
                    break;
                default:
                    response.ApplicationStatus = lastUploadedValidFilesCompanyDetailsUploadDatetime > response.LastSubmittedFile?.SubmittedDateTime
                        ? ApplicationStatusType.SubmittedAndHasRecentFileUpload
                        : ApplicationStatusType.SubmittedToRegulator;
                    break;
            }
        }

        return response;
    }
}