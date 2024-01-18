using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionEventsGet;

[TestClass]
public class RegulatorPoMDecisionSubmissionEventsGetQueryHandlerTests
{
    private readonly Guid _submissionId = Guid.NewGuid();
    private Mock<IQueryRepository<RegulatorPoMDecisionEvent>> _pomSubmissionQueryRepositoryMock;
    private Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventQueryRepositoryMock;
    private RegulatorPoMDecisionSubmissionEventsGetQueryHandler _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _pomSubmissionQueryRepositoryMock = new Mock<IQueryRepository<RegulatorPoMDecisionEvent>>();
        _submissionEventQueryRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _systemUnderTest = new RegulatorPoMDecisionSubmissionEventsGetQueryHandler(
        _pomSubmissionQueryRepositoryMock.Object,
        AutoMapperHelpers.GetMapper());
    }

    [TestMethod]
    public async Task Handle_IdFilteredReturnsTwoGetResponses()
    {
        // Arrange
        var fileId01 = Guid.NewGuid();
        var fileId02 = Guid.NewGuid();
        var fileId03 = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var comment001 = "Test Comment 001";
        var dateCreated = DateTime.Now;

        var query = new RegulatorPoMDecisionSubmissionEventsGetQuery
        {
            LastSyncTime = DateTime.UtcNow.AddDays(-6),
        };

        var decisionList = new List<RegulatorPoMDecisionEvent>
        {
            new()
            {
                Decision = RegulatorDecision.Approved,
                Comments = comment001,
                IsResubmissionRequired = false,
                FileId = fileId01,
                Created = dateCreated,
                SubmissionId = _submissionId,
                Id = eventId
            },
            new()
            {
                Decision = RegulatorDecision.Rejected,
                Comments = "Test Comment 003",
                IsResubmissionRequired = true,
                FileId = fileId03,
                Created = DateTime.Now.AddDays(-2),
                SubmissionId = _submissionId
            },
            new()
            {
                Decision = RegulatorDecision.Rejected,
                Comments = "Test Comment 002",
                IsResubmissionRequired = true,
                FileId = fileId02,
                Created = DateTime.Now.AddDays(-1),
                SubmissionId = _submissionId
            },
            new()
            {
                Decision = RegulatorDecision.Approved,
                Comments = "Test Comment 004 This should not be in the results",
                IsResubmissionRequired = true,
                FileId = Guid.NewGuid(),
                Created = DateTime.Now.AddMonths(-2),
                SubmissionId = Guid.NewGuid()
            }
        };

        var eventList = new List<SubmittedEvent>
        {
            new()
            {
                UserId = Guid.NewGuid(),
                SubmissionId = eventId,
                Created = DateTime.Now.AddDays(-5)
            }
        };

        _pomSubmissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<RegulatorPoMDecisionEvent, bool>>>()))
            .Returns(decisionList.BuildMock);

        _submissionEventQueryRepositoryMock
           .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
           .Returns(eventList.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().HaveCount(3);
        var orderedResult = result.Value.OrderBy(x => x.Created).ToList();
        var eventResult = orderedResult[0] as RegulatorDecisionGetResponse;
        eventResult.SubmissionId.Should().Be(_submissionId);
        eventResult.Decision.Should().Be(RegulatorDecision.Approved.ToString());
        eventResult.Comments.Should().Be(comment001);
        eventResult.IsResubmissionRequired.Should().BeFalse();
        eventResult.Created.Should().Be(dateCreated);
        eventResult.FileId.Should().Be(fileId01);
    }

    [TestMethod]
    public async Task Handle_DateAndIdFiltered_ReturnsSingleGetResponse()
    {
        // Arrange
        var fileId01 = Guid.NewGuid();
        var fileId02 = Guid.NewGuid();
        var fileId03 = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var query = new RegulatorPoMDecisionSubmissionEventsGetQuery
        {
            LastSyncTime = DateTime.UtcNow.AddDays(-6),
        };

        var decisionList = new List<RegulatorPoMDecisionEvent>
        {
            new()
            {
                Decision = RegulatorDecision.Approved,
                Comments = "Test Comment 001",
                IsResubmissionRequired = false,
                FileId = fileId01,
                Created = DateTime.Now.AddDays(-2),
                SubmissionId = _submissionId
            },
            new()
            {
                Decision = RegulatorDecision.Rejected,
                Comments = "Test Comment 003",
                IsResubmissionRequired = true,
                FileId = fileId03,
                Created = DateTime.Now.AddDays(-15),
                SubmissionId = _submissionId
            },
            new()
            {
                Decision = RegulatorDecision.Rejected,
                Comments = "Test Comment 002",
                IsResubmissionRequired = true,
                FileId = fileId02,
                Created = DateTime.Now.AddDays(-20),
                SubmissionId = _submissionId
            }
        };

        var eventList = new List<SubmittedEvent>
        {
            new()
            {
                UserId = Guid.NewGuid(),
                SubmissionId = eventId,
                Created = DateTime.Now.AddDays(-25)
            }
        };

        _pomSubmissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<RegulatorPoMDecisionEvent, bool>>>()))
            .Returns(decisionList.BuildMock);

        _submissionEventQueryRepositoryMock
           .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
           .Returns(eventList.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().HaveCount(1);
        var decisionResult = result.Value[0] as RegulatorDecisionGetResponse;
        decisionResult.SubmissionId.Should().Be(_submissionId);
        decisionResult.FileId.Should().Be(fileId01);
    }

    [TestMethod]
    public async Task Handle_IdFiltered_ReturnsFourSubmittedEventGetResponse()
    {
        // Arrange
        var fileId01 = Guid.NewGuid();
        var fileId02 = Guid.NewGuid();
        var fileId03 = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var comment001 = "Test Comment 001";
        var dateCreated = DateTime.Now;

        var query = new RegulatorPoMDecisionSubmissionEventsGetQuery
        {
            LastSyncTime = DateTime.UtcNow.AddDays(-6),
        };

        var decisionList = new List<RegulatorPoMDecisionEvent>
        {
            new()
            {
                Decision = RegulatorDecision.Approved,
                Comments = comment001,
                IsResubmissionRequired = false,
                FileId = fileId01,
                Created = dateCreated,
                SubmissionId = _submissionId,
                Id = eventId
            },
            new()
            {
                Decision = RegulatorDecision.Rejected,
                Comments = "Test Comment 003",
                IsResubmissionRequired = true,
                FileId = fileId03,
                Created = DateTime.Now.AddDays(-2),
                SubmissionId = _submissionId
            },
            new()
            {
                Decision = RegulatorDecision.Rejected,
                Comments = "Test Comment 002",
                IsResubmissionRequired = true,
                FileId = fileId02,
                Created = DateTime.Now.AddDays(-1),
                SubmissionId = _submissionId
            },
            new()
            {
                Decision = RegulatorDecision.Approved,
                Comments = "Test Comment 004 This should not be in the results",
                IsResubmissionRequired = true,
                FileId = Guid.NewGuid(),
                Created = DateTime.Now.AddDays(-3),
                SubmissionId = Guid.NewGuid()
            }
        };

        var eventList = new List<SubmittedEvent>
        {
            new()
            {
                UserId = Guid.NewGuid(),
                SubmissionId = eventId,
                Created = DateTime.Now
            }
        };

        _pomSubmissionQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<RegulatorPoMDecisionEvent, bool>>>()))
            .Returns(decisionList.BuildMock);

        _submissionEventQueryRepositoryMock
           .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
           .Returns(eventList.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().HaveCount(4);
        var orderedResult = result.Value.OrderBy(x => x.Created).ToList();
        var eventResult = orderedResult[0] as RegulatorDecisionGetResponse;
        eventResult.SubmissionId.Should().Be(_submissionId);
        eventResult.Decision.Should().Be(RegulatorDecision.Approved.ToString());
        eventResult.Comments.Should().Be(comment001);
        eventResult.IsResubmissionRequired.Should().BeFalse();
        eventResult.Created.Should().Be(dateCreated);
        eventResult.FileId.Should().Be(fileId01);
    }
}