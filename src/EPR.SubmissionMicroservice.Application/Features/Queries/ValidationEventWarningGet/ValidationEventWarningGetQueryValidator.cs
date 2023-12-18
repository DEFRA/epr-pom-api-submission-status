using FluentValidation;

namespace EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventWarningGet;

public class ValidationEventWarningGetQueryValidator : AbstractValidator<ValidationEventWarningGetQuery>
{
    public ValidationEventWarningGetQueryValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty().NotEqual(Guid.Empty);
    }
}