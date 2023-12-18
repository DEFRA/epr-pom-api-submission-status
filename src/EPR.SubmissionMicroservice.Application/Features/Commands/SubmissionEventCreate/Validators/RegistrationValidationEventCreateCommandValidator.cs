namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

using Data.Entities.Submission;
using Data.Repositories.Queries.Interfaces;
using FluentValidation;

public class RegistrationValidationEventCreateCommandValidator : AbstractValidator<RegistrationValidationEventCreateCommand>
{
    public RegistrationValidationEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new AbstractValidationEventCreateCommandValidator(queryRepository));
    }
}