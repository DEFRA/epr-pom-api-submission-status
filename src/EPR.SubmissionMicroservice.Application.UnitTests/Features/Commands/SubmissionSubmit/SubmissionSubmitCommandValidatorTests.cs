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
            UserId = Guid.NewGuid()
        };

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}