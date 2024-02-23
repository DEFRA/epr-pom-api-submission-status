namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionsPeriodGet;

using Application.Features.Queries.SubmissionsPeriodGet;
using Data.Enums;

[TestClass]
public class SubmissionsPeriodGetQueryValidatorTests
{
    private readonly SubmissionsPeriodGetQueryValidator _systemUnderTest = new();

    [TestMethod]
    public void Validator_ContainsErrorForSubmissionType_WhenTypeIsNotProvided()
    {
        // Arrange
        var query = new SubmissionsPeriodGetQuery
        {
            OrganisationId = Guid.NewGuid(),
            Year = 2023
        };

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor("Type")
            .WithErrorMessage("'Type' must not be empty.");

        result.ShouldHaveValidationErrorFor("Type")
            .WithErrorMessage("Please enter a valid submission type");
    }

    [TestMethod]
    public void Validator_ContainsErrorForOrganisationId_WhenOrganisationIdIsNotProvided()
    {
        // Arrange
        var query = new SubmissionsPeriodGetQuery
        {
            Type = SubmissionType.Producer
        };

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor("OrganisationId")
            .WithErrorMessage("'Organisation Id' must not be empty.");
    }

    [TestMethod]
    public void Validator_ContainsErrorForComplianceSchemeId_WhenComplianceSchemeIdIsSendEmpty()
    {
        // Arrange
        var query = new SubmissionsPeriodGetQuery
        {
            Type = SubmissionType.Producer,
            OrganisationId = Guid.NewGuid(),
            Year = 2023,
            ComplianceSchemeId = Guid.Empty
        };

        // Act
        var result = _systemUnderTest.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor("ComplianceSchemeId")
            .WithErrorMessage("'Compliance Scheme Id' must not be equal to '00000000-0000-0000-0000-000000000000'.");
    }
}
