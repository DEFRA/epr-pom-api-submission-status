using EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventWarningGet;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.ValidationEventWarningGet;

[TestClass]
public class ProducerValidationEventWarningGetQueryValidatorTests
{
    private readonly ValidationEventWarningGetQueryValidator _systemUnderTest = new();

    [TestMethod]
    public void Validator_ShouldNotHaveAnyErrors_WhenValidSubmissionIdIsProvided()
    {
        // Arrange
        // Act
        var result = _systemUnderTest.TestValidate(new ValidationEventWarningGetQuery(Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void Validator_ShouldContainValidationError_WhenEmptySubmissionIdIsProvided()
    {
        // Arrange
        // Act
        var result = _systemUnderTest.TestValidate(new ValidationEventWarningGetQuery(Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionId);
    }
}