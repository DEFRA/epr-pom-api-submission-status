namespace EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;

using FluentValidation;

public class SubmissionSubmitCommandValidator : AbstractValidator<SubmissionSubmitCommand>
{
    private static readonly string[] KnownRegulatorNations =
    {
        "GB-ENG",
        "GB-SCT",
        "GB-WLS",
        "GB-NIR",
    };

    public SubmissionSubmitCommandValidator()
    {
        RuleFor(p => p.SubmissionId).NotEmpty().WithMessage("Submission Id is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        // RegulatorNation is optional at API level (POM submits don't have it) but must be a
        // valid GB-XXX when provided. The command handler enforces its presence for the
        // Registration flow before publishing the fees-calc notification.
        RuleFor(x => x.RegulatorNation)
            .Must(BeKnownRegulatorNation).WithMessage("RegulatorNation must be one of GB-ENG, GB-SCT, GB-WLS, GB-NIR.")
            .When(x => !string.IsNullOrWhiteSpace(x.RegulatorNation));
    }

    private static bool BeKnownRegulatorNation(string? value) =>
        !string.IsNullOrWhiteSpace(value) && KnownRegulatorNations.Contains(value);
}
