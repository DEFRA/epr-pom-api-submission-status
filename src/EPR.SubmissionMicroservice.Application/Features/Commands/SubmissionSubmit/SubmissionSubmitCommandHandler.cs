using EPR.Common.Logging.Constants;
using EPR.Common.Logging.Models;
using EPR.Common.Logging.Services;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Data;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Commands.Interfaces;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;

public class SubmissionSubmitCommandHandler(
    ICommandRepository<Submission> submissionCommandRepository,
    ILogger<SubmissionSubmitCommandHandler> logger,
    IQueryRepository<Submission> submissionQueryRepository,
    ICommandRepository<AbstractSubmissionEvent> submissionEventCommandRepository,
    SubmissionContext submissionContext,
    IPomSubmissionEventHelper pomSubmissionEventHelper,
    ILoggingService loggingService,
    ISubmissionEventsValidator submissionEventValidator)
    : IRequestHandler<SubmissionSubmitCommand, ErrorOr<Unit>>
{
    public async Task<ErrorOr<Unit>> Handle(SubmissionSubmitCommand command, CancellationToken cancellationToken)
    {
        var submissionId = command.SubmissionId;
        var fileId = command.FileId;
        var userId = command.UserId;
        var submittedBy = command.SubmittedBy;

        try
        {
            var submission = await submissionQueryRepository.GetByIdAsync(submissionId, cancellationToken);

            var isFileIdForValidFile = submission!.SubmissionType is SubmissionType.Producer
                ? await pomSubmissionEventHelper.VerifyFileIdIsForValidFileAsync(submissionId, fileId, cancellationToken)
                : await submissionEventValidator.IsSubmissionValidAsync(submissionId, fileId, cancellationToken);

            if (!isFileIdForValidFile)
            {
                logger.LogInformation("File id {fileId} is not for a valid file", fileId);
                return Error.Failure();
            }

            submission.IsSubmitted = true;
            submission.IsResubmission = command.IsResubmission;
            submission.AppReferenceNumber = command.AppReferenceNumber;
            submission.RegistrationJourney = command.RegistrationJourney;
            submissionCommandRepository.Update(submission);

            var submittedEvent = new SubmittedEvent
            {
                SubmissionId = submissionId,
                FileId = fileId,
                UserId = userId,
                SubmittedBy = submittedBy,
                IsResubmission = command.IsResubmission,
                RegistrationJourney = submission.RegistrationJourney
            };

            await submissionEventCommandRepository.AddAsync(submittedEvent);
            await submissionContext.SaveChangesAsync(cancellationToken);
            await CreateProtectiveMonitoringEvent(submissionId, userId, fileId);

            logger.LogInformation("Submission with id {submissionId} submitted by user {userId}.", submissionId, userId);
            return Unit.Value;
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "An error occurred when submitting the submission with id {submissionId}", submissionId);
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
            await loggingService.SendEventAsync(userId, protectiveMonitoringEvent);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred creating the protective monitoring event");
        }
    }
}