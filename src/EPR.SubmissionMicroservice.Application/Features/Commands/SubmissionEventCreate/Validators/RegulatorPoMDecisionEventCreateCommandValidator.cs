using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

public class RegulatorPoMDecisionEventCreateCommandValidator : AbstractValidator<RegulatorPoMDecisionEventCreateCommand>
{
    public RegulatorPoMDecisionEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));

        RuleFor(x => x.Decision).IsInEnum();
        RuleFor(x => x.FileId).NotEmpty().NotEqual(Guid.Empty);
        When(x => x.Decision == RegulatorDecision.Rejected, () =>
        {
            RuleFor(x => x.Comments).NotEmpty().NotEqual(string.Empty);
        });
    }
}