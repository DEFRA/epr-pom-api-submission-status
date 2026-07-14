using EPR.SubmissionMicroservice.Application.Logging;
using EPR.SubmissionMicroservice.Application.Messaging.Publishing.RegistrationSubmittedForRegulatorApproval;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

using System;
using AutoMapper;
using Common.Logging.Constants;
using Common.Logging.Models;
using Common.Logging.Services;
using Data.Entities.SubmissionEvent;
using Data.Enums;
using Data.Repositories.Commands.Interfaces;
using ErrorOr;
using MediatR;
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
    private readonly ILoggingService _loggingService;
    private readonly IMapper _mapper;
    private readonly ILogger<SubmissionEventCreateCommandHandler> _logger;
    private readonly IPublisher _publisher;

    public SubmissionEventCreateCommandHandler(
        ICommandRepository<AbstractSubmissionEvent> commandRepository,
        ILoggingService loggingService,
        IMapper mapper,
        ILogger<SubmissionEventCreateCommandHandler> logger,
        IPublisher publisher)
    {
        _commandRepository = commandRepository;
        _loggingService = loggingService;
        _mapper = mapper;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(PackagingResubmissionReferenceNumberCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
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
        return await AbstractHandle(command, cancellationToken);
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

    private async Task<ErrorOr<SubmissionEventCreateResponse>> AbstractHandle(
        AbstractSubmissionEventCreateCommand command,
        CancellationToken cancellationToken)
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