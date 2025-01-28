namespace EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionUploadedFileGet;

using System.Diagnostics.CodeAnalysis;
using FluentValidation;

[ExcludeFromCodeCoverage]
public class SubmissionUploadedFileGetQueryValidator : AbstractValidator<SubmissionUploadedFileGetQuery>
{
    public SubmissionUploadedFileGetQueryValidator()
    {
        RuleFor(x => x.FileId).NotEmpty().NotEqual(Guid.Empty);
        RuleFor(x => x.SubmissionId).NotEmpty().NotEqual(Guid.Empty);
    }
}
