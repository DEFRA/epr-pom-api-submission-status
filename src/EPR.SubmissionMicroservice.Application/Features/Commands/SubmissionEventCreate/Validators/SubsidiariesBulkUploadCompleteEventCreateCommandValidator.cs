using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

public class SubsidiariesBulkUploadCompleteEventCreateCommandValidator : AbstractValidator<SubsidiariesBulkUploadCompleteEventCreateCommand>
{
    public SubsidiariesBulkUploadCompleteEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));
    }
}