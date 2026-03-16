using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common.Interfaces;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionGet;
using EPR.SubmissionMicroservice.Data.Enums;
using MediatR;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionGet;

[TestClass]
public class HydrateSubmissionBehaviourTests
{
    private Mock<ISubmissionHydrationService> _submissionHydrationServiceMock;

    [TestInitialize]
    public void SetUp()
    {
        _submissionHydrationServiceMock = new Mock<ISubmissionHydrationService>();
    }

    [TestMethod]
    public async Task Handle_WhenResponseIsError_ShouldReturnErrorWithoutCallingHydrationService()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var testError = Error.Custom(1, "TestError", "Test error description");
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(testError);

        var behaviour = new HydrateSubmissionBehaviour(_submissionHydrationServiceMock.Object);

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(testError);
        _submissionHydrationServiceMock.Verify(x => x.Hydrate(It.IsAny<AbstractSubmissionGetResponse>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_WhenResponseIsSuccess_ShouldCallHydrationServiceAndReturnSubmission()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var submission = CreateTestSubmission(submissionType: SubmissionType.Producer, submissionPeriod: "2025");
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submission);

        var behaviour = new HydrateSubmissionBehaviour(_submissionHydrationServiceMock.Object);

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(submission);
        _submissionHydrationServiceMock.Verify(x => x.Hydrate(submission), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WhenResponseIsSuccessWithRegistration_ShouldCallHydrationService()
    {
        // Arrange
        var request = new SubmissionGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var cancellationToken = CancellationToken.None;
        var submission = CreateTestSubmission(submissionType: SubmissionType.Registration, submissionPeriod: "Apr2026");
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<AbstractSubmissionGetResponse>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submission);

        var behaviour = new HydrateSubmissionBehaviour(_submissionHydrationServiceMock.Object);

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(submission);
        _submissionHydrationServiceMock.Verify(x => x.Hydrate(submission), Times.Once);
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
