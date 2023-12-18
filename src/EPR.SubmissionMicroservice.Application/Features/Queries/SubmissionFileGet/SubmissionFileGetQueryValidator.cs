namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionFileGet;

using FluentValidation;

public class SubmissionFileGetQueryValidator : AbstractValidator<SubmissionFileGetQuery>
{
    public SubmissionFileGetQueryValidator()
    {
        RuleFor(x => x.FileId).NotEmpty().NotEqual(Guid.Empty);
    }
}
