using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Enums;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;

public class SubmissionFileValidator(
    IValidationEventHelper validationEventHelper,
    IPomSubmissionEventHelper pomSubmissionEventHelper,
    ISubmissionEventsValidator submissionEventValidator)
    : ISubmissionFileValidator
{
    public async Task<bool> IsFileIdForValidFileAsync(Submission submission, Guid fileId, CancellationToken cancellationToken)
    {
        return submission.SubmissionType is SubmissionType.Producer
            ? await pomSubmissionEventHelper.VerifyFileIdIsForValidFileAsync(submission.Id, fileId, cancellationToken)
            : await submissionEventValidator.IsSubmissionValidAsync(submission.Id, fileId, cancellationToken);
    }
    
    public async Task<AntivirusResultEvent?> GetLatestAntivirusResultAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        return await validationEventHelper.GetLatestAntivirusResult(submissionId, cancellationToken);
    }
}
