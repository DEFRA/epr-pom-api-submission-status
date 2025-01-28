using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

public class PackagingDataResubmissionFeePaymentEventCreateCommandValidator : AbstractValidator<PackagingDataResubmissionFeePaymentEventCreateCommand>
{
    public PackagingDataResubmissionFeePaymentEventCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));

        RuleFor(x => x.PaymentMethod).NotNull().NotEmpty().WithMessage("'PaymentMethod' is mandatory");

        RuleFor(x => x.PaymentStatus).NotNull().NotEmpty().WithMessage("'PaymentStatus' is mandatory");

        RuleFor(x => x.PaidAmount).NotNull().NotEmpty().WithMessage("'PaidAmount' is mandatory");
    }
}
