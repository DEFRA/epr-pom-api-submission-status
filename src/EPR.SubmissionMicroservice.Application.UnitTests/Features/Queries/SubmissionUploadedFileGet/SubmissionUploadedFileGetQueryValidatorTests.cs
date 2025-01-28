namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionUploadedFileGet;

using Application.Features.Queries.SubmissionUploadedFileGet;
using FluentValidation.TestHelper;

[TestClass]
public class SubmissionUploadedFileGetQueryValidatorTests
{
    private readonly SubmissionUploadedFileGetQueryValidator _systemUnderTest = new();

    [TestMethod]
    public void Validator_DoesNotContainErrors_WhenRequiredPropertiesAreProvidedAndValid()
    {
        // Arrange
        var query = new SubmissionUploadedFileGetQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void Validator_ContainsErrorForFileId_WhenFileIdValueIsDefault()
    {
        // Arrange
        var query = new SubmissionUploadedFileGetQuery(Guid.Empty, Guid.Empty);

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileId)
            .WithErrorMessage("'File Id' must not be empty.");
    }
}