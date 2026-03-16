namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionsGet;

using Application.Features.Queries.Common;
using Application.Features.Queries.Common.Interfaces;
using Application.Features.Queries.SubmissionsGet;
using Data.Enums;
using MediatR;
using AutoFixture;

[TestClass]
public class HydrateSubmissionsBehaviourTests
{
    private Mock<ISubmissionHydrationService> _submissionHydrationServiceMock;
    private readonly IFixture _fixture = new Fixture();

    [TestInitialize]
    public void SetUp()
    {
        _submissionHydrationServiceMock = new Mock<ISubmissionHydrationService>();
    }

    [TestMethod]
    public async Task Handle_WhenResponseIsError_ShouldReturnErrorWithoutCallingHydrationService()
    {
        // Arrange
        var request = _fixture.Create<SubmissionsGetQuery>();
        var cancellationToken = CancellationToken.None;
        var testError = Error.Custom(1, "TestError", "Test error description");
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<List<AbstractSubmissionGetResponse>>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(testError);

        var behaviour = new HydrateSubmissionsBehaviour(_submissionHydrationServiceMock.Object);

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(testError);
        _submissionHydrationServiceMock.Verify(x => x.Hydrate(It.IsAny<AbstractSubmissionGetResponse>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_WhenResponseIsEmpty_ShouldReturnEmptyListWithoutCallingHydrationService()
    {
        // Arrange
        var request = _fixture.Create<SubmissionsGetQuery>();
        var cancellationToken = CancellationToken.None;
        var emptyList = new List<AbstractSubmissionGetResponse>();
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<List<AbstractSubmissionGetResponse>>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(emptyList);

        var behaviour = new HydrateSubmissionsBehaviour(_submissionHydrationServiceMock.Object);

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
        _submissionHydrationServiceMock.Verify(x => x.Hydrate(It.IsAny<AbstractSubmissionGetResponse>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_WhenResponseHasSingleSubmission_ShouldHydrateThatSubmission()
    {
        // Arrange
        var request = _fixture.Create<SubmissionsGetQuery>();
        var cancellationToken = CancellationToken.None;
        var submission = CreateTestSubmission(submissionType: SubmissionType.Registration, submissionPeriod: "Apr2026");
        var submissions = new List<AbstractSubmissionGetResponse> { submission };
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<List<AbstractSubmissionGetResponse>>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submissions);

        var behaviour = new HydrateSubmissionsBehaviour(_submissionHydrationServiceMock.Object);

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        _submissionHydrationServiceMock.Verify(x => x.Hydrate(submission), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WhenResponseHasMultipleSubmissions_ShouldHydrateEachSubmission()
    {
        // Arrange
        var request = _fixture.Create<SubmissionsGetQuery>();
        var cancellationToken = CancellationToken.None;
        var submission1 = CreateTestSubmission(submissionType: SubmissionType.Registration, submissionPeriod: "Apr2026");
        var submission2 = CreateTestSubmission(submissionType: SubmissionType.Producer, submissionPeriod: "Apr2026");
        var submission3 = CreateTestSubmission(submissionType: SubmissionType.Registration, submissionPeriod: "Apr2025");
        var submissions = new List<AbstractSubmissionGetResponse> { submission1, submission2, submission3 };
        var requestHandlerDelegate = new Mock<RequestHandlerDelegate<ErrorOr<List<AbstractSubmissionGetResponse>>>>();
        requestHandlerDelegate.Setup(x => x()).ReturnsAsync(submissions);

        var behaviour = new HydrateSubmissionsBehaviour(_submissionHydrationServiceMock.Object);

        // Act
        var result = await behaviour.Handle(request, requestHandlerDelegate.Object, cancellationToken);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
        _submissionHydrationServiceMock.Verify(x => x.Hydrate(submission1), Times.Once);
        _submissionHydrationServiceMock.Verify(x => x.Hydrate(submission2), Times.Once);
        _submissionHydrationServiceMock.Verify(x => x.Hydrate(submission3), Times.Once);
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
