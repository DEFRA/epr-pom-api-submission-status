using EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.GetRegistrationApplicationDetails;

[TestClass]
public class GetRegistrationApplicationDetailsQueryValidatorTests
{
    private GetRegistrationApplicationDetailsQueryValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetRegistrationApplicationDetailsQueryValidator();
    }

    [TestMethod]
    public void Validate_ShouldBeValid_WhenOrganisationIdAndSubmissionPeriodAreValid()
    {
        // Arrange
        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public void Validate_ShouldBeInvalid_WhenOrganisationIdIsEmpty()
    {
        // Arrange
        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.Empty,
            SubmissionPeriod = "2024-Q1"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "'Organisation Id' must not be empty.");
    }

    [TestMethod]
    public void Validate_ShouldBeInvalid_WhenSubmissionPeriodIsEmpty()
    {
        // Arrange
        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = string.Empty
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .And.Contain(e => e.ErrorMessage == "Please enter a valid submission period");
    }

    [TestMethod]
    public void Validate_ShouldBeInvalid_WhenSubmissionPeriodIsNull()
    {
        // Arrange
        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = null!
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .And.Contain(e => e.ErrorMessage == "Please enter a valid submission period");
    }

    [TestMethod]
    public void Validate_ShouldBeInvalid_WhenBothOrganisationIdAndSubmissionPeriodAreInvalid()
    {
        // Arrange
        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.Empty,
            SubmissionPeriod = string.Empty
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "'Organisation Id' must not be empty.");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Please enter a valid submission period");
    }

    [TestMethod]
    public void Validate_ShouldBeInvalid_WhenOrganisationIdIsValidButSubmissionPeriodIsInvalid()
    {
        // Arrange
        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = string.Empty // Invalid SubmissionPeriod
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .And.Contain(e => e.ErrorMessage == "Please enter a valid submission period");
    }

    [TestMethod]
    public void Validate_ShouldBeValid_WhenAdditionalFieldsArePresentButValid()
    {
        // Arrange
        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}