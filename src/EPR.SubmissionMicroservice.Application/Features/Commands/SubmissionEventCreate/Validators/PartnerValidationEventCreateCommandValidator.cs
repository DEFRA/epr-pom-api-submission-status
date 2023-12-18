namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

public class PartnerValidationEventCreateCommandValidator : AbstractValidator<PartnerValidationEventCreateCommand>
{
    public PartnerValidationEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new AbstractValidationEventCreateCommandValidator(queryRepository));
    }
}