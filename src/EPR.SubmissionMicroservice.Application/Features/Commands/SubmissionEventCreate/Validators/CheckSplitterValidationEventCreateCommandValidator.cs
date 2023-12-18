namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

using Data.Entities.Submission;
using Data.Repositories.Queries.Interfaces;
using FluentValidation;

public class CheckSplitterValidationEventCreateCommandValidator : AbstractValidator<CheckSplitterValidationEventCreateCommand>
{
    public CheckSplitterValidationEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new AbstractValidationEventCreateCommandValidator(queryRepository));

        RuleFor(x => x.DataCount).GreaterThanOrEqualTo(0);
    }
}