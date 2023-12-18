namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionGet;

using Application.Features.Queries.SubmissionGet;
using FluentValidation.TestHelper;

[TestClass]
public class SubmissionGetQueryValidatorTests
{
    private readonly SubmissionGetQueryValidator _systemUnderTest = new();

    [TestMethod]
    public void Validator_DoesNotContainErrors_WhenRequiredPropertiesAreProvidedAndValid()
    {
        // Arrange
        var query = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void Validator_ContainsErrorForId_WhenIdValueIsDefault()
    {
        // Arrange
        var query = new SubmissionGetQuery(Guid.Empty, Guid.NewGuid());

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("'Id' must not be empty.");
    }

    [TestMethod]
    public void Validator_ContainsErrorForOrganisationId_WhenOrganisationIdIsDefault()
    {
        // Arrange
        var query = new SubmissionGetQuery(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganisationId)
            .WithErrorMessage("'Organisation Id' must not be empty.");
    }
}