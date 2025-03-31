namespace EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;

[TestClass]
public class GetPackagingResubmissionApplicationDetailsQueryValidatorTests
{
    private GetPackagingResubmissionApplicationDetailsQueryValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetPackagingResubmissionApplicationDetailsQueryValidator();
    }

    [TestMethod]
    public void Validate_ShouldBeInValid_WhenOrganisationIdIsEmpty()
    {
        // Arrange
        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.Empty,
            SubmissionPeriods = new List<string> { "Jan - Jun 2024" }
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void Validate_ShouldBeInValid_WhenSubmissionPeriodIsNull()
    {
        // Arrange
        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.Empty
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    public void Validate_ShouldBeValid_WhenOrganisationIdAndSubmissionPeriodAreNotEmpty()
    {
        // Arrange
        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "Jan - Jun 2024" }
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}