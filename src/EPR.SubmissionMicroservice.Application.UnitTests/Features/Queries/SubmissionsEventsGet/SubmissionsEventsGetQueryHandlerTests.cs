namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionsEventsGet;

using System.Linq;
using Application.Features.Queries.Common;
using Application.Features.Queries.SubmissionsEventsGet;
using Data.Entities.AntivirusEvents;
using Data.Entities.SubmissionEvent;
using Data.Enums;
using Data.Repositories.Queries.Interfaces;

[TestClass]
public class SubmissionsEventsGetQueryHandlerTests
{
    private readonly Guid submissionId = Guid.NewGuid();
    private readonly DateTime lastSyncTime = DateTime.Now;
    private readonly Guid fileId = Guid.NewGuid();
    private readonly Guid userId = Guid.NewGuid();

    private Mock<IQueryRepository<SubmittedEvent>> _submittedEventQueryRepository;
    private Mock<IQueryRepository<RegulatorPoMDecisionEvent>> _regulatorPoMDecisionEventQueryRepository;
    private Mock<IQueryRepository<AntivirusCheckEvent>> _antivirusCheckEventQueryRepository;

    private SubmissionsEventsGetQueryHandler _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _submittedEventQueryRepository = new Mock<IQueryRepository<SubmittedEvent>>();
        _regulatorPoMDecisionEventQueryRepository = new Mock<IQueryRepository<RegulatorPoMDecisionEvent>>();
        _antivirusCheckEventQueryRepository = new Mock<IQueryRepository<AntivirusCheckEvent>>();

        _systemUnderTest = new SubmissionsEventsGetQueryHandler(
            _submittedEventQueryRepository.Object,
            _regulatorPoMDecisionEventQueryRepository.Object,
            _antivirusCheckEventQueryRepository.Object);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponses()
    {
        // Arrange
        var query = new SubmissionsEventsGetQuery(submissionId, lastSyncTime);

        var submittedEvent = new List<SubmittedEvent>
        {
            new()
            {
                Created = DateTime.Now,
                FileId = fileId,
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                SubmittedBy = "Test User",
                UserId = userId
            }
        };

        _submittedEventQueryRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<SubmittedEvent, bool>>>()))
            .Returns(submittedEvent.BuildMock());

        var regulatorPoMDecisionEvent = new List<RegulatorPoMDecisionEvent>
        {
            new()
            {
                UserId = userId,
                SubmissionId = submissionId,
                IsResubmissionRequired = true,
                Comments = string.Empty,
                Decision = RegulatorDecision.Accepted,
                Id = Guid.NewGuid(),
                FileId = fileId,
                Created = DateTime.Now
            }
        };

        _regulatorPoMDecisionEventQueryRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<RegulatorPoMDecisionEvent, bool>>>()))
            .Returns(regulatorPoMDecisionEvent.BuildMock());

        var antivirusCheckEvent = new List<AntivirusCheckEvent>
        {
            new()
            {
                Created = DateTime.Now,
                FileId = fileId,
                FileName = "TestFile.csv",
                Id = Guid.NewGuid(),
                RegistrationSetId = Guid.NewGuid(),
                SubmissionId = submissionId,
                UserId = userId,
            }
        };

        _antivirusCheckEventQueryRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AntivirusCheckEvent, bool>>>()))
            .Returns(antivirusCheckEvent.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.SubmittedEvents.Should().HaveCount(1);
        result.Value.RegulatorDecisionEvents.Should().HaveCount(1);
        result.Value.AntivirusCheckEvents.Should().HaveCount(1);

        result.Value.SubmittedEvents.Should().BeOfType<List<SubmittedEvents>>();
        result.Value.RegulatorDecisionEvents.Should().BeOfType<List<RegulatorDecisionEvents>>();
        result.Value.AntivirusCheckEvents.Should().BeOfType<List<AntivirusCheckEvents>>();

        _submittedEventQueryRepository
            .Verify(x => x.GetAll(x => x.SubmissionId == query.SubmissionId && x.Created > query.LastSyncTime), Times.Once);

        _regulatorPoMDecisionEventQueryRepository
            .Verify(x => x.GetAll(x => x.SubmissionId == query.SubmissionId && x.Created > query.LastSyncTime), Times.Once);

        _antivirusCheckEventQueryRepository
            .Verify(x => x.GetAll(x => x.SubmissionId == query.SubmissionId && x.Created > query.LastSyncTime), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsNoRecords_WhenEntitiesAreEmpty()
    {
        // Arrange
        var query = new SubmissionsEventsGetQuery(submissionId, lastSyncTime);

        var submittedEvent = new List<SubmittedEvent>();

        _submittedEventQueryRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<SubmittedEvent, bool>>>()))
            .Returns<Expression<Func<SubmittedEvent, bool>>>(expr => submittedEvent.Where(expr.Compile()).BuildMock());

        var regulatorPoMDecisionEvent = new List<RegulatorPoMDecisionEvent>();

        _regulatorPoMDecisionEventQueryRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<RegulatorPoMDecisionEvent, bool>>>()))
            .Returns<Expression<Func<RegulatorPoMDecisionEvent, bool>>>(expr => regulatorPoMDecisionEvent.Where(expr.Compile()).BuildMock());

        var antivirusCheckEvent = new List<AntivirusCheckEvent>();

        _antivirusCheckEventQueryRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AntivirusCheckEvent, bool>>>()))
            .Returns<Expression<Func<AntivirusCheckEvent, bool>>>(expr => antivirusCheckEvent.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.SubmittedEvents.Should().HaveCount(0);
        result.Value.RegulatorDecisionEvents.Should().HaveCount(0);
        result.Value.AntivirusCheckEvents.Should().HaveCount(0);

        _submittedEventQueryRepository
            .Verify(x => x.GetAll(x => x.SubmissionId == query.SubmissionId && x.Created > query.LastSyncTime), Times.Once);

        _regulatorPoMDecisionEventQueryRepository
            .Verify(x => x.GetAll(x => x.SubmissionId == query.SubmissionId && x.Created > query.LastSyncTime), Times.Once);

        _antivirusCheckEventQueryRepository
            .Verify(x => x.GetAll(x => x.SubmissionId == query.SubmissionId && x.Created > query.LastSyncTime), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ThrowsException_WhenSubmittedEventGiveException()
    {
        // Arrange
        var query = new SubmissionsEventsGetQuery(Guid.Empty, DateTime.MinValue);

        var submittedEvent = new List<SubmittedEvent>();

        _submittedEventQueryRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<SubmittedEvent, bool>>>()))
            .Throws(new Exception("Sql Query exception"));

        // Act / Assert
        await _systemUnderTest.Invoking(x => x.Handle(query, CancellationToken.None))
            .Should()
            .ThrowAsync<Exception>()
            .WithMessage("Sql Query exception");

        _submittedEventQueryRepository
            .Verify(x => x.GetAll(x => x.SubmissionId == query.SubmissionId && x.Created > query.LastSyncTime), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsError_WhenInvalidLastSyncTimeIsProvided()
    {
        // Arrange
        var query = new SubmissionsEventsGetQuery(submissionId, DateTime.Parse("01-01-9999"));

        var submittedEvent = new List<SubmittedEvent>();

        _submittedEventQueryRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<SubmittedEvent, bool>>>()))
            .Returns(submittedEvent.BuildMock());

        var regulatorPoMDecisionEvent = new List<RegulatorPoMDecisionEvent>();

        _regulatorPoMDecisionEventQueryRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<RegulatorPoMDecisionEvent, bool>>>()))
            .Returns(regulatorPoMDecisionEvent.BuildMock());

        var antivirusCheckEvent = new List<AntivirusCheckEvent>();

        _antivirusCheckEventQueryRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AntivirusCheckEvent, bool>>>()))
            .Returns(antivirusCheckEvent.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
    }
}