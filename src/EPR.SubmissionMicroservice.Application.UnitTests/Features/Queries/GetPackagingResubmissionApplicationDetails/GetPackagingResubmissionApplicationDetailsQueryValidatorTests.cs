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
            SubmissionPeriod = "Jan - Jun 2024"
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
            OrganisationId = Guid.Empty,
            SubmissionPeriod = null
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
            SubmissionPeriod = "Jan - Jun 2024"
        };

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}