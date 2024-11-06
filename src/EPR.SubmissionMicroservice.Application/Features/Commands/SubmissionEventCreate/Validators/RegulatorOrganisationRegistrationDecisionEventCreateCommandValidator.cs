using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

public class RegulatorOrganisationRegistrationDecisionEventCreateCommandValidator : AbstractValidator<RegulatorOrganisationRegistrationDecisionEventCreateCommand>
{
    public RegulatorOrganisationRegistrationDecisionEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));

        RuleFor(x => x.Decision).IsInEnum();
        When(x => x.Decision == RegulatorDecision.Rejected || x.Decision == RegulatorDecision.Queried || x.Decision == RegulatorDecision.Cancelled, () =>
        {
            RuleFor(x => x.Comments).NotEmpty().NotEqual(string.Empty);
        });
    }
}
