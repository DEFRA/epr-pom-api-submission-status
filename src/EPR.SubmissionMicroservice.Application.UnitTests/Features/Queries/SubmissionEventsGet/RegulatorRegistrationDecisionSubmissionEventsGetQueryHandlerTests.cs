namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionEventsGet;

using System.Linq.Expressions;
using AutoMapper;
using Data.Entities.SubmissionEvent;
using Data.Repositories.Queries.Interfaces;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

[TestClass]
public class RegulatorRegistrationDecisionSubmissionEventsGetQueryHandlerTests
{
    private readonly Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventQueryRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    private RegulatorRegistrationDecisionSubmissionEventsGetQueryHandler _systemUnderTest;

    [TestInitialize]
    public void Setup()
    {
        _systemUnderTest = new RegulatorRegistrationDecisionSubmissionEventsGetQueryHandler(
            _submissionEventQueryRepositoryMock.Object,
            _mapperMock.Object);
    }

    [TestMethod]
    public async Task Handle_WhenNoRegulatorRegistrationDecisionEvents_ReturnsOk()
    {
        // Arrange
        var request = new RegulatorRegistrationDecisionSubmissionEventsGetQuery();
        var regulatorRegistrationDecisionSubmissionEvents = new List<RegulatorRegistrationDecisionEvent>()
            .BuildMock();

        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(regulatorRegistrationDecisionSubmissionEvents);

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_WhenRegulatorRegistrationDecisionEventsExist_ReturnsOk()
    {
        // Arrange
        var request = new RegulatorRegistrationDecisionSubmissionEventsGetQuery();
        var regulatorRegistrationDecisionSubmissionEvents = new List<AbstractSubmissionEvent>
        {
            new RegulatorRegistrationDecisionEvent()
            {
                Created = DateTime.Now
            },
            new RegulatorRegistrationDecisionEvent()
            {
                Created = DateTime.Now
            },
        };

        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(regulatorRegistrationDecisionSubmissionEvents.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Count.Should().Be(2);
    }
}