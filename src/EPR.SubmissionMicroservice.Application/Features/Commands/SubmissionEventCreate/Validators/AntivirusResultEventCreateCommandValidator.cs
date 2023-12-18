namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

using Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

public class AntivirusResultEventCreateCommandValidator : AbstractValidator<AntivirusResultEventCreateCommand>
{
    public AntivirusResultEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));

        RuleFor(x => x.FileId).NotEmpty().NotEqual(Guid.Empty);
        RuleFor(x => x.AntivirusScanResult).IsInEnum();
    }
}
