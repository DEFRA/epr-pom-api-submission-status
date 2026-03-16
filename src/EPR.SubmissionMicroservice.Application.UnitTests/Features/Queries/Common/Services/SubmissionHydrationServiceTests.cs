namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.Common.Services;

using Application.Features.Queries.Common;
using Application.Features.Queries.Common.Services;
using Data.Enums;

[TestClass]
public class SubmissionHydrationServiceTests
{
    private SubmissionHydrationService _sut;

    [TestInitialize]
    public void SetUp()
    {
        _sut = new SubmissionHydrationService();
    }

    [TestMethod]
    public void Hydrate_WhenSubmissionTypeIsNotRegistration_ShouldNotModifySubmission()
    {
        // Arrange
        var submission = CreateTestSubmission(submissionType: SubmissionType.Producer, submissionPeriod: "Apr2026");
        var originalYear = submission.RegistrationYear;
        var originalJourney = submission.RegistrationJourney;

        // Act
        _sut.Hydrate(submission);

        // Assert
        submission.RegistrationYear.Should().Be(originalYear);
        submission.RegistrationJourney.Should().Be(originalJourney);
    }

    [TestMethod]
    public void Hydrate_WhenSubmissionTypeIsRegistrationWithValidYear_ShouldSetRegistrationYear()
    {
        // Arrange
        var submission = CreateTestSubmission(submissionType: SubmissionType.Registration, submissionPeriod: "Apr2026");

        // Act
        _sut.Hydrate(submission);

        // Assert
        submission.RegistrationYear.Should().Be(2026);
    }

    [TestMethod]
    public void Hydrate_WhenSubmissionTypeIsRegistrationWith2025Year_ShouldSetRegistrationYearCorrectly()
    {
        // Arrange
        var submission = CreateTestSubmission(submissionType: SubmissionType.Registration, submissionPeriod: "Apr2025");

        // Act
        _sut.Hydrate(submission);

        // Assert
        submission.RegistrationYear.Should().Be(2025);
    }

    [TestMethod]
    public void Hydrate_WhenSubmissionTypeIsRegistrationWithInvalidYear_ShouldNotSetRegistrationYear()
    {
        // Arrange
        var submission = CreateTestSubmission(submissionType: SubmissionType.Registration, submissionPeriod: "PXXX");

        // Act
        _sut.Hydrate(submission);

        // Assert
        submission.RegistrationYear.Should().BeNull();
    }

    [TestMethod]
    public void Hydrate_WhenSubmissionPeriodLessThan4Chars_ShouldNotSetRegistrationYear()
    {
        // Arrange
        var submission = CreateTestSubmission(submissionType: SubmissionType.Registration, submissionPeriod: "P");

        // Act
        _sut.Hydrate(submission);

        // Assert
        submission.RegistrationYear.Should().BeNull();
        submission.RegistrationJourney.Should().BeNull();
    }

    [TestMethod]
    public void Hydrate_WhenSubmissionTypeIsRegistrationWith2026AndComplianceSchemeAndNullJourney_ShouldSetRegistrationJourneyToCsoLargeProducer()
    {
        // Arrange
        var complianceSchemeId = Guid.NewGuid();
        var submission = CreateTestSubmission(
            submissionType: SubmissionType.Registration,
            submissionPeriod: "Apr2026",
            complianceSchemeId: complianceSchemeId,
            registrationJourney: null);

        // Act
        _sut.Hydrate(submission);

        // Assert
        submission.RegistrationYear.Should().Be(2026);
        submission.RegistrationJourney.Should().Be(RegistrationJourney.CsoLargeProducer.ToString());
    }

    [TestMethod]
    public void Hydrate_WhenSubmissionTypeIsRegistrationWith2026AndNoComplianceScheme_ShouldNotSetRegistrationJourney()
    {
        // Arrange
        var submission = CreateTestSubmission(
            submissionType: SubmissionType.Registration,
            submissionPeriod: "Apr2026",
            complianceSchemeId: null,
            registrationJourney: null);

        // Act
        _sut.Hydrate(submission);

        // Assert
        submission.RegistrationYear.Should().Be(2026);
        submission.RegistrationJourney.Should().BeNull();
    }

    [TestMethod]
    public void Hydrate_WhenSubmissionTypeIsRegistrationWith2026AndExistingJourney_ShouldNotOverwriteRegistrationJourney()
    {
        // Arrange
        var complianceSchemeId = Guid.NewGuid();
        var existingJourney = RegistrationJourney.DirectLargeProducer.ToString();
        var submission = CreateTestSubmission(
            submissionType: SubmissionType.Registration,
            submissionPeriod: "Apr2026",
            complianceSchemeId: complianceSchemeId,
            registrationJourney: existingJourney);

        // Act
        _sut.Hydrate(submission);

        // Assert
        submission.RegistrationYear.Should().Be(2026);
        submission.RegistrationJourney.Should().Be(existingJourney);
    }

    [TestMethod]
    public void Hydrate_WhenSubmissionTypeIsRegistrationWith2025AndComplianceScheme_ShouldNotSetRegistrationJourney()
    {
        // Arrange
        var complianceSchemeId = Guid.NewGuid();
        var submission = CreateTestSubmission(
            submissionType: SubmissionType.Registration,
            submissionPeriod: "Apr2025",
            complianceSchemeId: complianceSchemeId,
            registrationJourney: null);

        // Act
        _sut.Hydrate(submission);

        // Assert
        submission.RegistrationYear.Should().Be(2025);
        submission.RegistrationJourney.Should().BeNull();
    }

    private TestSubmissionGetResponse CreateTestSubmission(
        SubmissionType submissionType,
        string submissionPeriod,
        Guid? complianceSchemeId = null,
        string? registrationJourney = null)
    {
        return new TestSubmissionGetResponse
        {
            Id = Guid.NewGuid(),
            DataSourceType = DataSourceType.File,
            SubmissionType = submissionType,
            SubmissionPeriod = submissionPeriod,
            OrganisationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Created = DateTime.UtcNow,
            ValidationPass = true,
            HasWarnings = false,
            Errors = new List<string>(),
            IsSubmitted = true,
            ComplianceSchemeId = complianceSchemeId,
            RegistrationJourney = registrationJourney,
            RegistrationYear = null
        };
    }

    private class TestSubmissionGetResponse : AbstractSubmissionGetResponse
    {
        public override bool HasValidFile => true;
    }
}
