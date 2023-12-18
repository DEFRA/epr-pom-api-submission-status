namespace EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventErrorGet;

using FluentValidation;

public class ValidationEventErrorGetQueryValidator : AbstractValidator<ValidationEventErrorGetQuery>
{
    public ValidationEventErrorGetQueryValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty().NotEqual(Guid.Empty);
    }
}