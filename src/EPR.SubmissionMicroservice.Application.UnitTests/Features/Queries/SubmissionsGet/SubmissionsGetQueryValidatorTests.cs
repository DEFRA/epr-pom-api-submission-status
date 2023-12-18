namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionsGet;

using Application.Features.Queries.SubmissionsGet;
using Data.Enums;
using FluentValidation.TestHelper;

[TestClass]
public class SubmissionsGetQueryValidatorTests
{
    private readonly SubmissionsGetQueryValidator _systemUnderTest = new();

    [TestMethod]
    public void Validator_ContainsErrorForOrganisationId_WhenOrganisationIdIsNotProvided()
    {
        // Arrange
        var query = new SubmissionsGetQuery();

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor("OrganisationId")
            .WithErrorMessage("'Organisation Id' must not be empty.");
    }

    [TestMethod]
    public void Validator_DoesNotContainAnError_WhenQueryIsValid()
    {
        // Arrange
        var query = new SubmissionsGetQuery
        {
            OrganisationId = Guid.NewGuid(),
            Periods = new List<string>(),
            Type = SubmissionType.Registration
        };

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}