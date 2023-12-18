namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionCreate;

using Data.Constants;
using Data.Entities.Submission;
using Data.Repositories.Queries.Interfaces;
using FluentValidation;

public class SubmissionCreateCommandValidator : AbstractValidator<SubmissionCreateCommand>
{
    public SubmissionCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        RuleFor(p => p.Id)
            .NotEmpty()
            .NotEqual(Guid.Empty)
            .WithMessage("Submission Id is required.")
            .MustAsync(async (id, cancellation) => await queryRepository.GetByIdAsync(id, cancellation) == null)
            .WithMessage("Submission with id {PropertyValue} does exist.");

        RuleFor(x => x.OrganisationId).NotEmpty().NotEqual(default(Guid));
        RuleFor(x => x.UserId).NotEmpty().NotEqual(default(Guid));
        RuleFor(x => x.SubmissionPeriod).NotEmpty().MinimumLength(ValidationConstants.MinSubmissionPeriodLength);
        RuleFor(x => x.SubmissionType).IsInEnum();
        RuleFor(x => x.DataSourceType).IsInEnum();
    }
}