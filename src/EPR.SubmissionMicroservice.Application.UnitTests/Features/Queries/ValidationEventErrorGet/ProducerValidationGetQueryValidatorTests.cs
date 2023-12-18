using EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventErrorGet;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.ValidationEventErrorGet;

[TestClass]
public class ProducerValidationGetQueryValidatorTests
{
    private readonly ValidationEventErrorGetQueryValidator _systemUnderTest = new();

    [TestMethod]
    public void Validator_ShouldNotHaveAnyErrors_WhenValidSubmissionIdIsProvided()
    {
        // Arrange
        // Act
        var result = _systemUnderTest.TestValidate(new ValidationEventErrorGetQuery(Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void Validator_ShouldContainValidationError_WhenEmptySubmissionIdIsProvided()
    {
        // Arrange
        // Act
        var result = _systemUnderTest.TestValidate(new ValidationEventErrorGetQuery(Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionId);
    }
}