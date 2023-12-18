namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionFileGet;

using System.Linq.Expressions;
using Application.Features.Queries.SubmissionFileGet;
using Data.Entities.AntivirusEvents;
using Data.Entities.Submission;
using Data.Entities.SubmissionEvent;
using Data.Enums;
using Data.Repositories.Queries.Interfaces;
using ErrorOr;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

[TestClass]
public class SubmissionFileGetQueryHandlerTests
{
    private readonly Mock<IQueryRepository<Submission>> _submissionQueryRepositoryMock = new();
    private readonly Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventQueryRepositoryMock = new();

    private SubmissionFileGetQueryHandler _systemUnderTest;

    [TestInitialize]
    public void Setup()
    {
        _systemUnderTest = new SubmissionFileGetQueryHandler(
            _submissionQueryRepositoryMock.Object,
            _submissionEventQueryRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_WhenNoSubmissionExits_ReturnsError()
    {
        // Arrange
        var request = new SubmissionFileGetQuery(Guid.NewGuid());
        var antivirusCheckEvents = new List<AntivirusCheckEvent>()
            .BuildMock();

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission)null);
        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(antivirusCheckEvents.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Should().Be(Error.NotFound());
    }

    [TestMethod]
    public async Task Handle_WhenNoAntivirusCheckEventExits_ReturnsError()
    {
        // Arrange
        var request = new SubmissionFileGetQuery(Guid.NewGuid());
        var antivirusCheckEvents = new List<AntivirusCheckEvent>()
            .BuildMock();

        var submission = new Submission
        {
            SubmissionType = SubmissionType.Producer
        };
        var submissions = new[] { submission }.BuildMock();

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(antivirusCheckEvents.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Should().Be(Error.NotFound());
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionAndAntivirusCheckEventExit_ReturnsResponse()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            SubmissionType = SubmissionType.Producer
        };
        var submissions = new[] { submission }.BuildMock();
        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            SubmissionId = submission.Id,
            Errors = null,
            UserId = Guid.NewGuid(),
            FileId = fileId,
            FileName = "Test.csv",
            FileType = FileType.Pom,
        };
        var antivirusCheckEvents = new List<AntivirusCheckEvent> { antivirusCheckEvent };
        var request = new SubmissionFileGetQuery(fileId);

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(antivirusCheckEvents.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        var submissionFileGetResponse = result.Value.As<SubmissionFileGetResponse>();
        submissionFileGetResponse.FileId.Should().Be(fileId);
        submissionFileGetResponse.FileName.Should().Be("Test.csv");
        submissionFileGetResponse.SubmissionId.Should().Be(submission.Id);
    }
}