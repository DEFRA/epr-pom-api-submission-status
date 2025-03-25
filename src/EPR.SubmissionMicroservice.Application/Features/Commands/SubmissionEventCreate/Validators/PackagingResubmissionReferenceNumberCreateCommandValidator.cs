using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate.Validators;

public class PackagingResubmissionReferenceNumberCreateCommandValidator : AbstractValidator<PackagingResubmissionReferenceNumberCreateCommand>
{
    public PackagingResubmissionReferenceNumberCreateCommandValidator(IQueryRepository<Submission> queryRepository)
    {
        Include(new SubmissionEventCreateCommandValidator(queryRepository));

        RuleFor(x => x.PackagingResubmissionReferenceNumber).NotNull().NotEmpty().WithMessage("'Packaging Resubmission ReferenceNumber' is mandatory");
    }
}
