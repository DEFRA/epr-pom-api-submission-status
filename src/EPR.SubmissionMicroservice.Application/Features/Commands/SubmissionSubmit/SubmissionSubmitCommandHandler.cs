using EPR.Common.Logging.Constants;
using EPR.Common.Logging.Models;
using EPR.Common.Logging.Services;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Application.Logging;
using EPR.SubmissionMicroservice.Application.Messaging.Publishing.RegistrationSubmittedForFeesCalculation;
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
    IValidationEventHelper validationEventHelper,
    ICommandRepository<AbstractSubmissionEvent> submissionEventCommandRepository,
    SubmissionContext submissionContext,
    IPomSubmissionEventHelper pomSubmissionEventHelper,
    ILoggingService loggingService,
    ISubmissionEventsValidator submissionEventValidator,
    IPublisher publisher)
    : IRequestHandler<SubmissionSubmitCommand, ErrorOr<Unit>>
{
    public async Task<ErrorOr<Unit>> Handle(SubmissionSubmitCommand command, CancellationToken cancellationToken)
    {
        using (logger.BeginScope("Submitting file for fee calculation"))
        using (logger.AddScopedData(new Dictionary<string, object>
               {
                   ["SubmissionId"] = command.SubmissionId,
                   ["FileId"] = command.FileId,
                   ["UserId"] = command.UserId,
                   ["SubmittedBy"] = command.SubmittedBy,
                   ["AppReferenceNumber"] = command.AppReferenceNumber,
                   ["IsResubmission"] = command.IsResubmission
               }))
        {
            var submissionId = command.SubmissionId;
            var fileId = command.FileId;
            var userId = command.UserId;
            var submittedBy = command.SubmittedBy;

            try
            {
                var submission = await submissionQueryRepository.GetByIdAsync(submissionId, cancellationToken);

                var isFileIdForValidFile = submission!.SubmissionType is SubmissionType.Producer
                    ? await pomSubmissionEventHelper.VerifyFileIdIsForValidFileAsync(submissionId, fileId,
                        cancellationToken)
                    : await submissionEventValidator.IsSubmissionValidAsync(submissionId, fileId, cancellationToken);

                if (!isFileIdForValidFile)
                {
                    logger.LogInformation("File id {fileId} is not for a valid file", fileId);
                    return Error.Failure();
                }

                submission.IsSubmitted = true;
                submission.IsResubmission = command.IsResubmission;

                if (string.IsNullOrEmpty(submission.AppReferenceNumber))
                {
                    submission.AppReferenceNumber = command.AppReferenceNumber;
                }

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

                // get blob name
                var antivirusResult =
                    await validationEventHelper.GetLatestAntivirusResult(command.SubmissionId, cancellationToken);

                if (antivirusResult is null || string.IsNullOrWhiteSpace(antivirusResult.BlobName))
                {
                    logger.LogError($"Antivirus result event blob name is null. Antivirus event is { (antivirusResult  is null ? "not null" : "null")}");
                    return Error.Failure();
                }

                // publish submitted event to message bus
                var message = new RegistrationSubmittedForFeesCalculationNotification(submissionId,
                    antivirusResult.BlobName,
                    submission.ComplianceSchemeId, submission.SubmissionPeriod, submittedEvent.Created);
                await publisher.Publish(message, cancellationToken);

                logger.LogInformation("Submission with id {submissionId} submitted by user {userId}.", submissionId,
                    userId);
                return Unit.Value;
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, "An error occurred when submitting the submission with id {submissionId}",
                    submissionId);
                return Error.Unexpected();
            }
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