namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionUploadedFileGet;

using System.Linq.Expressions;
using Application.Features.Queries.SubmissionUploadedFileGet;
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
public class SubmissionUploadedFileGetQueryHandlerTests
{
    private readonly Mock<IQueryRepository<Submission>> _submissionQueryRepositoryMock = new();
    private readonly Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventQueryRepositoryMock = new();

    private SubmissionUploadedFileGetQueryHandler _systemUnderTest;

    [TestInitialize]
    public void Setup()
    {
        _systemUnderTest = new SubmissionUploadedFileGetQueryHandler(
            _submissionQueryRepositoryMock.Object,
            _submissionEventQueryRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_WhenNoSubmissionExits_ReturnsError()
    {
        // Arrange
        var request = new SubmissionUploadedFileGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var antivirusResultEvents = new List<AntivirusResultEvent>()
            .BuildMock();

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission)null);
        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(antivirusResultEvents.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().HaveCount(1);
        result.Errors.FirstOrDefault().Should().Be(Error.NotFound());
    }

    [TestMethod]
    public async Task Handle_WhenNoAntivirusResultEventFound_ReturnsNotFoundError()
    {
        // Arrange
        var request = new SubmissionUploadedFileGetQuery(Guid.NewGuid(), Guid.NewGuid());
        var antivirusResultEvents = new List<AntivirusResultEvent>()
            .BuildMock();

        var submission = new Submission
        {
            SubmissionType = SubmissionType.Producer
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(antivirusResultEvents.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().HaveCount(1);
        result.Errors.FirstOrDefault().Should().Be(Error.NotFound());
    }

    [TestMethod]
    public async Task Handle_WhenAntivirusResultEventExists_AndNoErrors_ReturnsResponse()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var blobName = Guid.NewGuid().ToString();
        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            SubmissionType = SubmissionType.Producer
        };
        var antivirusResultEvent = new AntivirusResultEvent
        {
            SubmissionId = submission.Id,
            Errors = null,
            UserId = Guid.NewGuid(),
            FileId = fileId,
            BlobName = blobName,
            AntivirusScanResult = AntivirusScanResult.Success,
        };
        var antivirusResultEvents = new List<AntivirusResultEvent> { antivirusResultEvent };
        var request = new SubmissionUploadedFileGetQuery(fileId, submission.Id);

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(antivirusResultEvents.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        var submissionUploadedFileGetResponse = result.Value.As<SubmissionUploadedFileGetResponse>();
        submissionUploadedFileGetResponse.FileId.Should().Be(fileId);
        submissionUploadedFileGetResponse.BlobName.Should().Be(blobName);
        submissionUploadedFileGetResponse.SubmissionId.Should().Be(submission.Id);
        submissionUploadedFileGetResponse.AntivirusScanResult.Should().Be(AntivirusScanResult.Success);
        submissionUploadedFileGetResponse.Errors.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_WhenAntivirusResultEventExists_AndHasErrors_ReturnsResponse()
    {
        // Arrange
        string expectedErrorCode = "123";
        var fileId = Guid.NewGuid();
        var blobName = Guid.NewGuid().ToString();
        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            SubmissionType = SubmissionType.Registration
        };

        var antivirusResultEvent = new AntivirusResultEvent
        {
            SubmissionId = submission.Id,
            Errors = new List<string> { expectedErrorCode },
            UserId = Guid.NewGuid(),
            FileId = fileId,
            BlobName = blobName,
            AntivirusScanResult = AntivirusScanResult.Success,
        };
        var antivirusResultEvents = new List<AntivirusResultEvent> { antivirusResultEvent };
        var request = new SubmissionUploadedFileGetQuery(fileId, submission.Id);

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(antivirusResultEvents.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        var submissionUploadedFileGetResponse = result.Value.As<SubmissionUploadedFileGetResponse>();
        submissionUploadedFileGetResponse.FileId.Should().Be(fileId);
        submissionUploadedFileGetResponse.BlobName.Should().Be(blobName);
        submissionUploadedFileGetResponse.SubmissionId.Should().Be(submission.Id);
        submissionUploadedFileGetResponse.AntivirusScanResult.Should().Be(AntivirusScanResult.Success);
        submissionUploadedFileGetResponse.Errors.Should().OnlyContain(x => x == expectedErrorCode);
    }
}