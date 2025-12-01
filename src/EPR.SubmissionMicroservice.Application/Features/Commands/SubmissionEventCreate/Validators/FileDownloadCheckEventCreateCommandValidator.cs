using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

public class FileDownloadCheckEventCreateCommandValidator : AbstractValidator<FileDownloadCheckEventCreateCommand>
{
    public FileDownloadCheckEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));

        RuleFor(x => x.FileId).NotEmpty().NotEqual(Guid.Empty);
        RuleFor(x => x.ContentScan).NotEmpty();
    }
}
