using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionGet;
using EPR.SubmissionMicroservice.Data.Enums;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionGet;

[TestClass]
public class HydrateSubmissionBehaviourTests
{
    [TestMethod]
    public async Task Handle_WhenResponseIsError_ShouldReturnError()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var testError = Error.Custom(1, "TestError", "Test error description");
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(testError);

        var behaviour = new HydrateSubmissionBehaviour();

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(testError);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsNotRegistration_ShouldReturnSubmissionUnchanged()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var submission = CreateTestSubmission(submissionType: SubmissionType.Producer, submissionPeriod: "2025");
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submission);

        var behaviour = new HydrateSubmissionBehaviour();

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(submission);
        result.Value.RegistrationYear.Should().BeNull();
        result.Value.RegistrationJourney.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsRegistrationWithValidYear_ShouldSetRegistrationYear()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var submission = CreateTestSubmission(submissionType: SubmissionType.Registration, submissionPeriod: "Apr2026");
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submission);

        var behaviour = new HydrateSubmissionBehaviour();

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.RegistrationYear.Should().Be(2026);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsRegistrationWith2025Year_ShouldSetRegistrationYearCorrectly()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var submission = CreateTestSubmission(submissionType: SubmissionType.Registration, submissionPeriod: "Apr2025");
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submission);

        var behaviour = new HydrateSubmissionBehaviour();

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.RegistrationYear.Should().Be(2025);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsRegistrationWithInvalidYear_ShouldNotSetRegistrationYear()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var submission = CreateTestSubmission(submissionType: SubmissionType.Registration, submissionPeriod: "PXXX");
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submission);

        var behaviour = new HydrateSubmissionBehaviour();

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.RegistrationYear.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsRegistrationWithShortPeriod_ShouldNotSetRegistrationYear()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var submission = CreateTestSubmission(submissionType: SubmissionType.Registration, submissionPeriod: "P");
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submission);

        var behaviour = new HydrateSubmissionBehaviour();

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.RegistrationYear.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsRegistrationWith2026AndComplianceSchemeAndNullJourney_ShouldSetRegistrationJourneyToCsoLargeProducer()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var complianceSchemeId = Guid.NewGuid();
        var submission = CreateTestSubmission(
            submissionType: SubmissionType.Registration,
            submissionPeriod: "Apr2026",
            complianceSchemeId: complianceSchemeId,
            registrationJourney: null);

        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submission);

        var behaviour = new HydrateSubmissionBehaviour();

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.RegistrationYear.Should().Be(2026);
        result.Value.RegistrationJourney.Should().Be(RegistrationJourney.CsoLargeProducer.ToString());
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsRegistrationWith2026AndNoComplianceScheme_ShouldNotSetRegistrationJourney()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var submission = CreateTestSubmission(
            submissionType: SubmissionType.Registration,
            submissionPeriod: "Apr2026",
            complianceSchemeId: null,
            registrationJourney: null);

        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submission);

        var behaviour = new HydrateSubmissionBehaviour();

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.RegistrationYear.Should().Be(2026);
        result.Value.RegistrationJourney.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsRegistrationWith2026AndExistingJourney_ShouldNotOverwriteRegistrationJourney()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var complianceSchemeId = Guid.NewGuid();
        var existingJourney = RegistrationJourney.DirectLargeProducer.ToString();
        var submission = CreateTestSubmission(
            submissionType: SubmissionType.Registration,
            submissionPeriod: "Apr2026",
            complianceSchemeId: complianceSchemeId,
            registrationJourney: existingJourney);

        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submission);

        var behaviour = new HydrateSubmissionBehaviour();

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.RegistrationYear.Should().Be(2026);
        result.Value.RegistrationJourney.Should().Be(existingJourney);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionTypeIsRegistrationWith2025AndComplianceScheme_ShouldNotSetRegistrationJourney()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var complianceSchemeId = Guid.NewGuid();
        var submission = CreateTestSubmission(
            submissionType: SubmissionType.Registration,
            submissionPeriod: "Apr2025",
            complianceSchemeId: complianceSchemeId,
            registrationJourney: null);

        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submission);

        var behaviour = new HydrateSubmissionBehaviour();

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.RegistrationYear.Should().Be(2025);
        result.Value.RegistrationJourney.Should().BeNull();
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
