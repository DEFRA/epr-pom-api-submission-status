namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionOrganisationDetailsGet;

using Application.Features.Queries.SubmissionOrganisationDetailsGet;
using FluentValidation.TestHelper;

[TestClass]
public class SubmissionOrganisationDetailsGetQueryValidatorTests
{
    private readonly SubmissionOrganisationDetailsGetQueryValidator _systemUnderTest = new();

    [TestMethod]
    public void Validator_DoesNotContainErrors_WhenRequiredPropertiesAreProvidedAndValid()
    {
        // Arrange
        var query = new SubmissionOrganisationDetailsGetQuery(Guid.NewGuid(), "test");

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void Validator_ContainsErrorForId_WhenIdValueIsDefault()
    {
        // Arrange
        var query = new SubmissionOrganisationDetailsGetQuery(Guid.Empty, "test");

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionId)
            .WithErrorMessage("'Submission Id' must not be empty.");
    }

    [TestMethod]
    public void Validator_ContainsErrorForOrganisationId_WhenBlobNameIdIsNull()
    {
        // Arrange
        var query = new SubmissionOrganisationDetailsGetQuery(Guid.NewGuid(), null);

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BlobName)
            .WithErrorMessage("'Blob Name' must not be empty.");
    }

    [TestMethod]
    public void Validator_ContainsErrorForOrganisationId_WhenBlobNameIdIsEmptyString()
    {
        // Arrange
        var query = new SubmissionOrganisationDetailsGetQuery(Guid.NewGuid(), string.Empty);

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BlobName)
            .WithErrorMessage("'Blob Name' must not be empty.");
    }
}