namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;

using Common.Logging.Constants;
using Common.Logging.Models;
using Common.Logging.Services;
using Data;
using Data.Entities.Submission;
using Data.Entities.SubmissionEvent;
using Data.Enums;
using Data.Repositories.Commands.Interfaces;
using Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Queries.Helpers.Interfaces;

public class SubmissionSubmitCommandHandler : IRequestHandler<SubmissionSubmitCommand, ErrorOr<Unit>>
{
    private readonly ICommandRepository<Submission> _submissionCommandRepository;
    private readonly ICommandRepository<AbstractSubmissionEvent> _submissionEventCommandRepository;
    private readonly SubmissionContext _submissionContext;
    private readonly IPomSubmissionEventHelper _pomSubmissionEventHelper;
    private readonly IRegistrationSubmissionEventHelper _registrationSubmissionEventHelper;
    private readonly IQueryRepository<Submission> _submissionQueryRepository;
    private readonly ILogger<SubmissionSubmitCommandHandler> _logger;
    private readonly ILoggingService _loggingService;

    public SubmissionSubmitCommandHandler(
        ICommandRepository<Submission> submissionCommandRepository,
        ILogger<SubmissionSubmitCommandHandler> logger,
        IQueryRepository<Submission> submissionQueryRepository,
        ICommandRepository<AbstractSubmissionEvent> submissionEventCommandRepository,
        SubmissionContext submissionContext,
        IPomSubmissionEventHelper pomSubmissionEventHelper,
        ILoggingService loggingService,
        IRegistrationSubmissionEventHelper registrationSubmissionEventHelper)
    {
        _submissionCommandRepository = submissionCommandRepository;
        _logger = logger;
        _submissionQueryRepository = submissionQueryRepository;
        _submissionEventCommandRepository = submissionEventCommandRepository;
        _submissionContext = submissionContext;
        _pomSubmissionEventHelper = pomSubmissionEventHelper;
        _loggingService = loggingService;
        _registrationSubmissionEventHelper = registrationSubmissionEventHelper;
    }

    public async Task<ErrorOr<Unit>> Handle(SubmissionSubmitCommand command, CancellationToken cancellationToken)
    {
        var submissionId = command.SubmissionId;
        var fileId = command.FileId;
        var userId = command.UserId;
        var submittedBy = command.SubmittedBy;

        try
        {
            var submission = await _submissionQueryRepository.GetByIdAsync(submissionId, cancellationToken);

            var isFileIdForValidFile = submission!.SubmissionType is SubmissionType.Producer
                ? await _pomSubmissionEventHelper.VerifyFileIdIsForValidFileAsync(submissionId, fileId, cancellationToken)
                : await _registrationSubmissionEventHelper.VerifyFileIdIsForValidFileAsync(submissionId, fileId, cancellationToken);

            if (!isFileIdForValidFile)
            {
                _logger.LogInformation("File id {fileId} is not for a valid file", fileId);
                return Error.Failure();
            }

            if (submission is not { IsSubmitted: true })
            {
                submission.IsSubmitted = true;
                _submissionCommandRepository.Update(submission);
            }

            var submittedEvent = new SubmittedEvent
            {
                SubmissionId = submissionId,
                FileId = fileId,
                UserId = userId,
                SubmittedBy = submittedBy
            };

            await _submissionEventCommandRepository.AddAsync(submittedEvent);
            await _submissionContext.SaveChangesAsync(cancellationToken);
            await CreateProtectiveMonitoringEvent(submissionId, userId, fileId);

            _logger.LogInformation("Submission with id {submissionId} submitted by user {userId}.", submissionId, userId);
            return Unit.Value;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "An error occurred when submitting the submission with id {submissionId}", submissionId);
            return Error.Unexpected();
        }
    }

    private async Task CreateProtectiveMonitoringEvent(Guid submissionId, Guid userId, Guid fileId)
    {
        try
        {
            var protectiveMonitoringEvent = new ProtectiveMonitoringEvent(
                submissionId,
                "epr_pom_api_submission_status",
                PmcCodes.Code0212,
                Priorities.NormalEvent,
                TransactionCodes.SubmissionSubmitted,
                "Submission submitted",
                $"SubmissionId: {submissionId}, FileId: {fileId}");
            await _loggingService.SendEventAsync(userId, protectiveMonitoringEvent);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occurred creating the protective monitoring event");
        }
    }
}