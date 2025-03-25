using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

public class PackagingResubmissionFeeViewCreateCommandValidator : AbstractValidator<PackagingResubmissionFeeViewCreateCommand>
{
    public PackagingResubmissionFeeViewCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));

        RuleFor(x => x.IsPackagingResubmissionFeeViewed).Equal(true).WithMessage("'Packaging Resubmission IsPackagingResubmissionFeeViewed' should be true");
    }
}