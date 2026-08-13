namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionSubmit;

using Application.Features.Commands.SubmissionSubmit;
using FluentAssertions;
using FluentValidation.TestHelper;

[TestClass]
public class SubmissionSubmitCommandValidatorTests
{
    private readonly SubmissionSubmitCommandValidator _systemUnderTest = new();

    [TestMethod]
    public async Task Validator_ReturnsErrors_WhenCommandIsInvalid()
    {
        // Arrange
        var command = new SubmissionSubmitCommand();

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.Errors.Select(x => x.ErrorMessage)
            .Should()
            .HaveCount(2)
            .And
            .Contain("Submission Id is required.")
            .And
            .Contain("User Id is required.");
    }

    [TestMethod]
    public async Task Validator_ReturnsSuccess_WhenCommandIsValid()
    {
        // Arrange
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AppReferenceNumber = "PEPR2601234",
            RegulatorNation = "GB-ENG",
        };

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task Validator_ReturnsSuccess_WhenRegulatorNationOmitted()
    {
        // RegulatorNation is only required for the Registration flow; the handler enforces
        // that. POM submissions omit it and the API-level validator must allow that.
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
        };

        var result = await _systemUnderTest.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.RegulatorNation);
    }

    [DataTestMethod]
    [DataRow("GB-ENG")]
    [DataRow("GB-SCT")]
    [DataRow("GB-WLS")]
    [DataRow("GB-NIR")]
    public async Task Validator_AcceptsKnownRegulatorNations(string regulatorNation)
    {
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AppReferenceNumber = "PEPR2601234",
            RegulatorNation = regulatorNation,
        };

        var result = await _systemUnderTest.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.RegulatorNation);
    }

    [DataTestMethod]
    [DataRow("gb-eng")]
    [DataRow("ENG")]
    [DataRow("GB-XXX")]
    [DataRow("regulator")]
    public async Task Validator_RejectsUnknownRegulatorNations(string regulatorNation)
    {
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AppReferenceNumber = "PEPR2601234",
            RegulatorNation = regulatorNation,
        };

        var result = await _systemUnderTest.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.RegulatorNation)
            .WithErrorMessage("RegulatorNation must be one of GB-ENG, GB-SCT, GB-WLS, GB-NIR.");
    }
}
