namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

using Data.Constants;
using Data.Entities.Submission;
using Data.Repositories.Queries.Interfaces;
using FluentValidation;

public class AntivirusCheckEventCreateCommandValidator : AbstractValidator<AntivirusCheckEventCreateCommand>
{
    public AntivirusCheckEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));

        RuleFor(x => x.FileId).NotEmpty().NotEqual(Guid.Empty);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(ValidationConstants.MaxFileNameLength);
        RuleFor(x => x.FileType).IsInEnum();
        When(x => x.RegistrationSetId.HasValue, () =>
        {
            RuleFor(x => x.RegistrationSetId).NotEmpty().NotEqual(Guid.Empty);
        });
    }
}