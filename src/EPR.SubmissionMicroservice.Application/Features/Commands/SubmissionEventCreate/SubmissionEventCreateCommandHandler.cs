using EPR.SubmissionMicroservice.Application.Logging;
using EPR.SubmissionMicroservice.Application.Messaging.Publishing.RegistrationSubmittedForRegulatorApproval;
using EPR.SubmissionMicroservice.Application.Messaging.Publishing.RegulatorRegistrationDecision;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

using System;
using AutoMapper;
using Common.Logging.Constants;
using Common.Logging.Models;
using Common.Logging.Services;
using Data.Entities.AntivirusEvents;
using Data.Entities.SubmissionEvent;
using Data.Enums;
using Data.Repositories.Commands.Interfaces;
using Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class SubmissionEventCreateCommandHandler :
    IRequestHandler<CheckSplitterValidationEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<ProducerValidationEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<RegistrationValidationEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<AntivirusCheckEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<AntivirusResultEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<RegulatorPoMDecisionEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<BrandValidationEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<PartnerValidationEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<RegulatorRegistrationDecisionEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    //IRequestHandler<RegulatorOrganisationRegistrationDecisionEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<RegistrationFeePaymentEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<RegistrationApplicationSubmittedEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<FileDownloadCheckEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<SubsidiariesBulkUploadCompleteEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<PackagingDataResubmissionFeePaymentEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<PackagingResubmissionReferenceNumberCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<PackagingResubmissionFeeViewCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<PackagingResubmissionApplicationSubmittedCreateCommand, ErrorOr<SubmissionEventCreateResponse>>
{
    private readonly ICommandRepository<AbstractSubmissionEvent> _commandRepository;
    private readonly IQueryRepository<AbstractSubmissionEvent> _eventQueryRepository;
    private readonly ILoggingService _loggingService;
    private readonly IMapper _mapper;
    private readonly ILogger<SubmissionEventCreateCommandHandler> _logger;
    private readonly IPublisher _publisher;

    public SubmissionEventCreateCommandHandler(
        ICommandRepository<AbstractSubmissionEvent> commandRepository,
        IQueryRepository<AbstractSubmissionEvent> eventQueryRepository,
        ILoggingService loggingService,
        IMapper mapper,
        ILogger<SubmissionEventCreateCommandHandler> logger,
        IPublisher publisher)
    {
        _commandRepository = commandRepository;
        _eventQueryRepository = eventQueryRepository;
        _loggingService = loggingService;
        _mapper = mapper;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(PackagingResubmissionReferenceNumberCreateCommand command, CancellationToken cancellationToken)
    {
        var cycleStart = await GetStartOfCycleAwaitingItsReferenceNumber(command.SubmissionId, cancellationToken);

        return await AbstractHandle(command, cancellationToken, cycleStart);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(PackagingResubmissionFeeViewCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(PackagingResubmissionApplicationSubmittedCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(AntivirusCheckEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(RegulatorPoMDecisionEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(FileDownloadCheckEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(RegulatorRegistrationDecisionEventCreateCommand command, CancellationToken cancellationToken)
    {
        var response = await AbstractHandle(command, cancellationToken);

        if (!response.IsError && TryMapRegulatorDecision(command.Decision, out var eventName))
        {
            var decisionDate = command.DecisionDate ?? DateTime.UtcNow;
            var notification = new RegulatorRegistrationDecisionNotification(command.SubmissionId, eventName, decisionDate);
            await _publisher.Publish(notification, cancellationToken);
        }

        return response;
    }

    private static bool TryMapRegulatorDecision(RegulatorDecision decision, out string eventName)
    {
        eventName = decision switch
        {
            RegulatorDecision.Accepted => "AcceptedByRegulator",
            RegulatorDecision.Rejected => "RejectedByRegulator",
            RegulatorDecision.Queried => "QueriedByRegulator",
            RegulatorDecision.Cancelled => "CancelledByRegulator",
            _ => null!,
        };

        return eventName is not null;
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(AntivirusResultEventCreateCommand command, CancellationToken cancellationToken)
    {
        command.AntivirusScanTrigger ??= AntivirusScanTrigger.Upload;

        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(CheckSplitterValidationEventCreateCommand command, CancellationToken cancellationToken)
    {
        var result = await AbstractHandle(command, cancellationToken);

        if (command.ValidationErrors.Count > 0)
        {
            var errorsList = command.ValidationErrors
                .SelectMany(x => x.ErrorCodes)
                .Distinct();
            LogAsync(command.SubmissionId, command.UserId.Value, string.Join(", ", errorsList));
        }

        return result;
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(ProducerValidationEventCreateCommand command, CancellationToken cancellationToken)
    {
        var result = await AbstractHandle(command, cancellationToken);

        if (command.ValidationErrors.Count > 0)
        {
            var errorsList = command.ValidationErrors
                .SelectMany(x => x.ErrorCodes)
                .Distinct();
            LogAsync(command.SubmissionId, command.UserId.Value, string.Join(", ", errorsList));
        }

        return result;
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(RegistrationValidationEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(BrandValidationEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(PartnerValidationEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(RegistrationFeePaymentEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(RegistrationApplicationSubmittedEventCreateCommand command, CancellationToken cancellationToken)
    {
        var response = await AbstractHandle(command, cancellationToken);

        if (!response.IsError)
        {
            var message = new RegistrationSubmittedForRegulatorApprovalNotification(command.SubmissionId, command.ApplicationReferenceNumber, command.SubmissionDate.Value);
            await _publisher.Publish(message, cancellationToken);
        }
        
        return response;
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(PackagingDataResubmissionFeePaymentEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(SubsidiariesBulkUploadCompleteEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    /// <summary>
    /// SUB-345: the moment a resubmission cycle opened, for a cycle whose reference number is only being
    /// raised now - after the work it covers - or null when the number is not late and belongs to this
    /// request's own time.
    /// </summary>
    /// <remarks>
    /// GetPackagingResubmissionApplicationDetails decides whether an upload belongs to a cycle by comparing it
    /// against that cycle's reference number: an upload predating the number belongs to the cycle before it, so
    /// a cycle with no upload after its number reports as having nothing uploaded into it. Numbering normally
    /// precedes a cycle's first upload, so that holds. Until this ticket, though, only the first resubmission
    /// was ever numbered, which leaves cycles that ran as far as a submitted file and a paid fee with no number
    /// at all. Numbered at the moment they are finally noticed, every event they contain predates the number
    /// and the whole cycle reports as unstarted, leaving the user outside a resubmission they had all but
    /// finished.
    /// <para>
    /// The ruling that closed the previous cycle is what opened this one, so the number is dated a minute ahead
    /// of the cycle's first upload and never earlier than that ruling. Nothing is invented: the date is taken
    /// from events the cycle already holds, and it only moves for a cycle left unnumbered, which the journey
    /// can no longer produce.
    /// </para>
    /// </remarks>
    private async Task<DateTime?> GetStartOfCycleAwaitingItsReferenceNumber(Guid submissionId, CancellationToken cancellationToken)
    {
        var submissionEvents = await _eventQueryRepository
            .GetAll(x => x.SubmissionId == submissionId)
            .ToListAsync(cancellationToken);

        var cycleOpeningDecisionEvent = submissionEvents.OfType<RegulatorPoMDecisionEvent>()
            .Where(x => x.Decision is RegulatorDecision.Accepted or RegulatorDecision.Approved or RegulatorDecision.Rejected)
            .MaxBy(x => x.Created);

        // No ruling means there is no earlier cycle to have been closed: this is the submission's first cycle,
        // and the number being raised is what opens it.
        if (cycleOpeningDecisionEvent is null)
        {
            return null;
        }

        // A cycle already holding a number of its own is not what this covers. Backdating a duplicate would
        // make it the earliest number the cycle has, and that is the one the query handler reports.
        var isCycleAlreadyNumbered = submissionEvents.OfType<PackagingResubmissionReferenceNumberCreatedEvent>()
            .Any(x => x.Created > cycleOpeningDecisionEvent.Created);

        if (isCycleAlreadyNumbered)
        {
            return null;
        }

        var firstUploadInCycle = submissionEvents.OfType<AntivirusCheckEvent>()
            .Where(x => x.FileType == FileType.Pom && x.Created > cycleOpeningDecisionEvent.Created)
            .MinBy(x => x.Created);

        // Nothing has been uploaded since the ruling, so the number is being raised as the cycle opens rather
        // than late, and the request's own time is the truthful date for it.
        if (firstUploadInCycle is null)
        {
            return null;
        }

        var oneMinuteBeforeTheFirstUpload = firstUploadInCycle.Created.AddMinutes(-1);

        return oneMinuteBeforeTheFirstUpload > cycleOpeningDecisionEvent.Created
            ? oneMinuteBeforeTheFirstUpload
            : cycleOpeningDecisionEvent.Created;
    }

    private async Task<ErrorOr<SubmissionEventCreateResponse>> AbstractHandle(
        AbstractSubmissionEventCreateCommand command,
        CancellationToken cancellationToken,
        DateTime? created = null)
    {
        using (_logger.BeginScope("Creating submission event"))
        using (_logger.AddScopedData(new Dictionary<string, object>
               {
                   ["SubmissionId"] = command.SubmissionId,
                   ["Type"] = command.Type,
                   ["UserId"] = command.UserId,
                   ["BlobName"] = command.BlobName,
                   ["BlobContainerName"] = command.BlobContainerName
               }))
        {
            var submissionEvent = _mapper.Map<AbstractSubmissionEvent>(command);

            if (created.HasValue)
            {
                // SUB-345: SubmissionContext stamps the request time over anything left at its default, so an
                // event dated from the cycle it belongs to has to carry that date in before it is saved.
                submissionEvent.Created = created.Value;

                _logger.LogInformation(
                    "Dating submission event from the start of the cycle it belongs to, {Created}, rather than from this request",
                    created.Value);
            }

            _logger.LogInformation("Storing submission event");
            await _commandRepository.AddAsync(submissionEvent);

            var success = await _commandRepository.SaveChangesAsync(cancellationToken);

            if (success)
            {
                _logger.LogInformation("Submission event was successfully stored with event id {SubmissionEventId}", submissionEvent.Id);
                return new SubmissionEventCreateResponse(submissionEvent.Id);
            }
            else
            {
                _logger.LogInformation("Failed to store submission event");
                return Error.Failure();
            }
        }
    }

    private async Task LogAsync(Guid sessionId, Guid userId, string additionalInfo)
    {
        try
        {
            await _loggingService.SendEventAsync(
                userId,
                new ProtectiveMonitoringEvent(
                    sessionId,
                    "epr_pom_api_submission_status",
                    PmcCodes.Code0210,
                    Priorities.UnusualEvent,
                    TransactionCodes.FileValidationFailed,
                    "Validation failed",
                    additionalInfo));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred creating the protective monitoring event");
        }
    }
}