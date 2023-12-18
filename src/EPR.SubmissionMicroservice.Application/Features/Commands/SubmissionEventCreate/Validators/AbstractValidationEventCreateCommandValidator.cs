namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

using Data.Entities.Submission;
using Data.Repositories.Queries.Interfaces;
using FluentValidation;

public class AbstractValidationEventCreateCommandValidator : AbstractValidator<AbstractValidationEventCreateCommand>
{
    public AbstractValidationEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));
    }
}