using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

public class RegulatorRegistrationDecisionEventCreateCommandValidator : AbstractValidator<RegulatorRegistrationDecisionEventCreateCommand>
{
    public RegulatorRegistrationDecisionEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));
        RuleFor(x => x.Decision).IsInEnum();

        When(x => !x.IsForOrganisationRegistration, () =>
        {
            RuleFor(x => x.FileId).NotEmpty().NotEqual(Guid.Empty);
            When(x => x.Decision == RegulatorDecision.Rejected, () =>
            {
                RuleFor(x => x.Comments).NotEmpty().NotEqual(string.Empty);
            });
        });
        When(x => x.IsForOrganisationRegistration, () =>
        {
            RuleFor(p => p.AppReferenceNumber).NotNull().NotEmpty().WithMessage("'App Reference Number' must not be empty.");
            RuleFor(x => x.Decision).NotEqual(RegulatorDecision.None);
            RuleFor(p => p.UserId).NotNull().NotEmpty().NotEqual(Guid.Empty).WithMessage("'User Id' must not be empty.");
        });
    }
}