namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionGet;

using Application.Features.Queries.SubmissionFileGet;
using FluentValidation.TestHelper;

[TestClass]
public class SubmissionFileGetQueryValidatorTests
{
    private readonly SubmissionFileGetQueryValidator _systemUnderTest = new();

    [TestMethod]
    public void Validator_DoesNotContainErrors_WhenRequiredPropertiesAreProvidedAndValid()
    {
        // Arrange
        var query = new SubmissionFileGetQuery(Guid.NewGuid());

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void Validator_ContainsErrorForFileId_WhenFileIdValueIsDefault()
    {
        // Arrange
        var query = new SubmissionFileGetQuery(Guid.Empty);

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileId)
            .WithErrorMessage("'File Id' must not be empty.");
    }
}