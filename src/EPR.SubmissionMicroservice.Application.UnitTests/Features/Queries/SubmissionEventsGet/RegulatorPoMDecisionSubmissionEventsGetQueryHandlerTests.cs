namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionEventsGet;

using System.Collections.Generic;
using System.Linq.Expressions;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using TestSupport.Helpers;

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
        _submissionEventQueryRepositoryMock.Object,
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

        var query = new RegulatorPoMDecisionSubmissionEventsGetQuery
        {
            SubmissionId = _submissionId,
            LastSyncTime = DateTime.UtcNow.AddMonths(-1)
        };

        var decisionList = new List<RegulatorPoMDecisionEvent>
        {
            new()
            {
                Decision = RegulatorDecision.Approved,
                Comments = "Test Comment 001",
                IsResubmissionRequired = false,
                FileId = fileId01,
                Created = DateTime.Now,
                SubmissionId = _submissionId
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
                Created = DateTime.Now.AddDays(-2),
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
        result.Value.Should().HaveCount(2);
        var orderedResult = result.Value.OrderBy(x => x.Created).ToList();
        var eventResult = orderedResult[0] as AbstractSubmissionEventGetResponse;
        var decisionResult = orderedResult[1] as RegulatorDecisionGetResponse;
        eventResult.SubmissionId.Should().Be(eventId);
        decisionResult.SubmissionId.Should().Be(_submissionId);
        decisionResult.FileId.Should().Be(fileId01);
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
            SubmissionId = _submissionId,
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
    public async Task Handle_IdFiltered_ReturnsSingleSubmittedEventGetResponse()
    {
        // Arrange
        var fileId01 = Guid.NewGuid();
        var fileId02 = Guid.NewGuid();
        var fileId03 = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var query = new RegulatorPoMDecisionSubmissionEventsGetQuery
        {
            SubmissionId = _submissionId,
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
                Created = DateTime.Now,
                SubmissionId = _submissionId
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
        result.Value.Should().HaveCount(2);
        var orderedResult = result.Value.OrderBy(x => x.Created).ToList();
        var eventResult = orderedResult[0] as AbstractSubmissionEventGetResponse;
        var decisionResult = orderedResult[1] as RegulatorDecisionGetResponse;
        eventResult.SubmissionId.Should().Be(eventId);
        decisionResult.SubmissionId.Should().Be(_submissionId);
        decisionResult.FileId.Should().Be(fileId01);
    }
}