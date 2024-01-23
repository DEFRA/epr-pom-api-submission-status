namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;

using AutoMapper;
using Common.Logging.Constants;
using Common.Logging.Models;
using Common.Logging.Services;
using Data.Entities.SubmissionEvent;
using Data.Repositories.Commands.Interfaces;
using EPR.SubmissionMicroservice.Data.Constants;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

public class SubmissionEventCreateCommandHandler :
    IRequestHandler<CheckSplitterValidationEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<ProducerValidationEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<RegistrationValidationEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<AntivirusCheckEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<AntivirusResultEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<RegulatorPoMDecisionEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<BrandValidationEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
    IRequestHandler<PartnerValidationEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>,
IRequestHandler<RegulatorRegistrationDecisionEventCreateCommand, ErrorOr<SubmissionEventCreateResponse>>
{
    private readonly ICommandRepository<AbstractSubmissionEvent> _commandRepository;
    private readonly ILoggingService _loggingService;
    private readonly IMapper _mapper;
    private readonly ILogger<SubmissionEventCreateCommandHandler> _logger;
    private readonly IFeatureManager _featureManager;

    public SubmissionEventCreateCommandHandler(
        ICommandRepository<AbstractSubmissionEvent> commandRepository,
        ILoggingService loggingService,
        IMapper mapper,
        ILogger<SubmissionEventCreateCommandHandler> logger,
        IFeatureManager featureManager)
    {
        _commandRepository = commandRepository;
        _loggingService = loggingService;
        _mapper = mapper;
        _logger = logger;
        _featureManager = featureManager;
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(AntivirusCheckEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(RegulatorPoMDecisionEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(RegulatorRegistrationDecisionEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(AntivirusResultEventCreateCommand command, CancellationToken cancellationToken)
    {
        return await AbstractHandle(command, cancellationToken);
    }

    public async Task<ErrorOr<SubmissionEventCreateResponse>> Handle(CheckSplitterValidationEventCreateCommand command, CancellationToken cancellationToken)
    {
        var result = await AbstractHandle(command, cancellationToken);

        if (command.ValidationErrors.Any())
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

        if (command.ValidationErrors.Any())
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

    private async Task<ErrorOr<SubmissionEventCreateResponse>> AbstractHandle(
        AbstractSubmissionEventCreateCommand command,
        CancellationToken cancellationToken)
    {
        var submissionEvent = _mapper.Map<AbstractSubmissionEvent>(command);

        await _commandRepository.AddAsync(submissionEvent);

        return await _commandRepository.SaveChangesAsync(cancellationToken)
            ? new SubmissionEventCreateResponse(submissionEvent.Id)
            : Error.Failure();
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