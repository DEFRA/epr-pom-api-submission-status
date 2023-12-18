namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

public class BrandValidationEventCreateCommandValidator : AbstractValidator<BrandValidationEventCreateCommand>
{
    public BrandValidationEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new AbstractValidationEventCreateCommandValidator(queryRepository));
    }
}