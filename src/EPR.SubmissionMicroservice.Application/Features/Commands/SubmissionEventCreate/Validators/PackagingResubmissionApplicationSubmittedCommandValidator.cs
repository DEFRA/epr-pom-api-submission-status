using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

public class PackagingResubmissionApplicationSubmittedCommandValidator : AbstractValidator<PackagingResubmissionApplicationSubmittedCreateCommand>
{
    public PackagingResubmissionApplicationSubmittedCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));

        RuleFor(x => x.IsResubmitted).Equal(true).WithMessage("'Packaging Resubmission IsResubmitted' should be true");
    }
}