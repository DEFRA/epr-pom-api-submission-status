using System.Linq.Expressions;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using TestSupport.Helpers;

namespace EPR.SubmissionMicroservice.Application.Tests.Features.Queries.SubmissionEventsGet;

[TestClass]
public class RegulatorPoMDecisionSubmissionEventsGetQueryHandlerTests
{
    private Mock<IQueryRepository<RegulatorPoMDecisionEvent>> _submissionEventQueryRepositoryMock;
    private RegulatorPoMDecisionSubmissionEventsGetQueryHandler _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _submissionEventQueryRepositoryMock = new Mock<IQueryRepository<RegulatorPoMDecisionEvent>>();
        _systemUnderTest = new RegulatorPoMDecisionSubmissionEventsGetQueryHandler(
            _submissionEventQueryRepositoryMock.Object,
            AutoMapperHelpers.GetMapper());
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponses()
    {
        // Arrange
        var query = new RegulatorPoMDecisionSubmissionEventsGetQuery
        {
            LastSyncTime = DateTime.Today.AddDays(-5)
        };

        var submissions = new List<RegulatorPoMDecisionEvent>
        {
            new()
            {
                SubmissionId = Guid.NewGuid(),
                Created = DateTime.Today.AddDays(-10)
            },
            new()
            {
                SubmissionId = Guid.NewGuid(),
                Created = DateTime.Today.AddDays(-1)
            }
        };

        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<RegulatorPoMDecisionEvent, bool>>>()))
            .Returns<Expression<Func<RegulatorPoMDecisionEvent, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
        result.Value[0].SubmissionId.Should().Be(submissions[1].SubmissionId);

        _submissionEventQueryRepositoryMock.Verify(x => x.GetAll(e => e.Type == EventType.RegulatorPoMDecision), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedSubmissionList_WhenNoPoMDecisionsFoundAfterLastSync()
    {
        // Arrange
        var query = new RegulatorPoMDecisionSubmissionEventsGetQuery
        {
            LastSyncTime = DateTime.Today
        };

        var submissions = new List<RegulatorPoMDecisionEvent>
        {
            new()
            {
                SubmissionId = Guid.NewGuid(),
                Created = DateTime.Today.AddDays(-10)
            },
            new()
            {
                SubmissionId = Guid.NewGuid(),
                Created = DateTime.Today.AddDays(-1)
            }
        };

        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<RegulatorPoMDecisionEvent, bool>>>()))
            .Returns<Expression<Func<RegulatorPoMDecisionEvent, bool>>>(expr => submissions.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().BeEmpty();

        _submissionEventQueryRepositoryMock.Verify(x => x.GetAll(e => e.Type == EventType.RegulatorPoMDecision), Times.Once);
    }
}