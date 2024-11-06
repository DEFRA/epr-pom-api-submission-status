using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionEventsGet;

[TestClass]
public class RegulatorOrganisationRegistrationDecisionEventGetQueryHandlerTests
{
    private readonly Guid _submissionId = Guid.NewGuid();
    private Mock<IQueryRepository<RegulatorOrganisationRegistrationDecisionEvent>> _registrationSubmissionQueryRepositoryMock;
    private Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventQueryRepositoryMock;
    private RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryHandler _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _registrationSubmissionQueryRepositoryMock = new Mock<IQueryRepository<RegulatorOrganisationRegistrationDecisionEvent>>();
        _submissionEventQueryRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _systemUnderTest = new RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryHandler(
                                _submissionEventQueryRepositoryMock.Object,
                                AutoMapperHelpers.GetMapper());
    }

    [TestMethod]
    public async Task Handle_IdUnfilteredReturnsTwoGetResponsesInMostRecentCreationOrder()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        var query = new RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery
        {
            LastSyncTime = DateTime.UtcNow.AddMonths(-1)
        };

        var decisionsList = new List<RegulatorOrganisationRegistrationDecisionEvent>
        {
            new()
            {
                Comments = "These are the old comments for a cancellation",
                Created = DateTime.Now - TimeSpan.FromMinutes(120),
                Decision = RegulatorDecision.Cancelled,
                SubmissionId = submissionId
            },
            new()
            {
                Comments = "These are the latest comments for a query",
                Created = DateTime.Now,
                Decision = RegulatorDecision.Queried,
                SubmissionId = submissionId
            },
            new()
            {
                Comments = "These are the latest comments for a query",
                Created = DateTime.Now - TimeSpan.FromDays(1),
                Decision = RegulatorDecision.Queried,
                SubmissionId = _submissionId
            }
        };

        _submissionEventQueryRepositoryMock
           .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
           .Returns(decisionsList.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().HaveCount(3);
        var newerResult = result.Value[0] as AbstractSubmissionEventGetResponse;
        var olderResult = result.Value[1] as RegulatorOrganisationRegistrationDecisionGetResponse;
        newerResult.SubmissionId.Should().Be(submissionId);
        olderResult.SubmissionId.Should().NotBe(_submissionId);
        newerResult.Created.Should().BeAfter(olderResult.Created);
    }

    [TestMethod]
    public async Task Handle_IdFilteredReturnsTwoGetResponsesInMostRecentCreationOrder()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        var query = new RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery
        {
            SubmissionId = submissionId,
            LastSyncTime = DateTime.UtcNow.AddMonths(-1)
        };

        var decisionsList = new List<RegulatorOrganisationRegistrationDecisionEvent>
        {
            new()
            {
                Comments = "These are the old comments for a cancellation",
                Created = DateTime.Now - TimeSpan.FromMinutes(120),
                Decision = RegulatorDecision.Cancelled,
                SubmissionId = submissionId
            },
            new()
            {
                Comments = "These are the latest comments for a query",
                Created = DateTime.Now,
                Decision = RegulatorDecision.Queried,
                SubmissionId = submissionId
            },
            new()
            {
                Comments = "These are the latest comments for a query",
                Created = DateTime.Now,
                Decision = RegulatorDecision.Queried,
                SubmissionId = _submissionId
            }
        };

        _submissionEventQueryRepositoryMock
           .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
           .Returns(decisionsList.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        var orderedResult = result.Value.OrderBy(x => x.Created).ToList();
        var newerResult = result.Value[0] as AbstractSubmissionEventGetResponse;
        var olderResult = result.Value[1] as RegulatorOrganisationRegistrationDecisionGetResponse;
        newerResult.SubmissionId.Should().Be(submissionId);
        olderResult.SubmissionId.Should().NotBe(_submissionId);
        newerResult.Created.Should().BeAfter(olderResult.Created);
    }
}