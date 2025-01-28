namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionEventsGet;

using System.Linq.Expressions;
using AutoMapper;
using Data.Entities.SubmissionEvent;
using Data.Repositories.Queries.Interfaces;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
using EPR.SubmissionMicroservice.Data.Enums;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

[TestClass]
public class RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryHandlerTests
{
    private readonly Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventQueryRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    private RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryHandler _systemUnderTest;

    [TestInitialize]
    public void Setup()
    {
        _systemUnderTest = new RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQueryHandler(
            _submissionEventQueryRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_WhenRegulatorRegistrationDecisionEventsExist_ReturnsOk()
    {
        // Arrange
        var request = new RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery();
        var regulatorRegistrationDecisionSubmissionEvents = new List<AbstractSubmissionEvent>
        {
            new RegulatorRegistrationDecisionEvent()
            {
                Created = DateTime.Now,
                AppReferenceNumber = "1234567890"
            },
            new RegulatorRegistrationDecisionEvent()
            {
                Created = DateTime.Now,
                AppReferenceNumber = "0987654321"
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
        result.Value[0].AppReferenceNumber.Should().Be("0987654321");
    }

    [TestMethod]
    public async Task Handle_WhenOnlyRegulatorRegistrationDecisionEventsExist_ReturnsExpectedResponses()
    {
        // Arrange
        var request = new RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery
        {
            LastSyncTime = DateTime.MinValue
        };

        var regulatorRegistrationDecisionEvents = new List<AbstractSubmissionEvent>
        {
            new RegulatorRegistrationDecisionEvent
            {
                SubmissionId = Guid.NewGuid(),
                Created = DateTime.UtcNow.AddDays(-1),
                AppReferenceNumber = "APP123",
                Comments = "Decision comment 1",
                Decision = RegulatorDecision.Accepted,
                DecisionDate = DateTime.UtcNow.AddDays(-1),
                RegistrationReferenceNumber = "REG123"
            },
            new RegulatorRegistrationDecisionEvent
            {
                SubmissionId = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                AppReferenceNumber = "APP456",
                Comments = "Decision comment 2",
                Decision = RegulatorDecision.Rejected,
                DecisionDate = DateTime.UtcNow,
                RegistrationReferenceNumber = "REG456"
            },
            new RegulatorRegistrationDecisionEvent
            {
                SubmissionId = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                AppReferenceNumber = "APP789",
                Comments = "Decision comment 3",
                Decision = RegulatorDecision.Approved,
                DecisionDate = DateTime.UtcNow,
                RegistrationReferenceNumber = "REG789"
            },
        };

        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(regulatorRegistrationDecisionEvents.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);

        var firstResponse = result.Value[result.Value.Count - 1];
        firstResponse.AppReferenceNumber.Should().Be("APP123");
        firstResponse.Comments.Should().Be("Decision comment 1");
        firstResponse.Decision.Should().Be("Granted");
        firstResponse.RegistrationReferenceNumber.Should().Be("REG123");
        firstResponse.Type.Should().Be("RegulatorRegistrationDecision");

        var secondResponse = result.Value[1];
        secondResponse.AppReferenceNumber.Should().Be("APP456");
        secondResponse.Comments.Should().Be("Decision comment 2");
        secondResponse.Decision.Should().Be("Refused");
        secondResponse.RegistrationReferenceNumber.Should().Be("REG456");
        secondResponse.Type.Should().Be("RegulatorRegistrationDecision");

        var thirdResponse = result.Value[0];
        thirdResponse.AppReferenceNumber.Should().Be("APP789");
        thirdResponse.Comments.Should().Be("Decision comment 3");
        thirdResponse.Decision.Should().Be("Granted");
        thirdResponse.RegistrationReferenceNumber.Should().Be("REG789");
        thirdResponse.Type.Should().Be("RegulatorRegistrationDecision");
    }

    [TestMethod]
    public async Task Handle_WhenNoEventsExist_ReturnsEmptyList()
    {
        // Arrange
        var request = new RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery
        {
            LastSyncTime = DateTime.UtcNow
        };

        var noEvents = new List<AbstractSubmissionEvent>();

        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(noEvents.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_WhenProducerEventsExist_ReturnsExpectedResponse()
    {
        // Arrange
        var request = new RegulatorOrganisationRegistrationDecisionSubmissionEventsGetQuery
        {
            LastSyncTime = DateTime.MinValue
        };

        var regulatorRegistrationDecisionEvents = new List<AbstractSubmissionEvent>
        {
            new RegistrationApplicationSubmittedEvent
            {
                SubmissionId = Guid.NewGuid(),
                Created = DateTime.UtcNow.AddDays(-1),
                ApplicationReferenceNumber = "APP123",
                Comments = "Producer comment 1",
                SubmissionDate = DateTime.UtcNow.AddDays(-1)
            }
        };

        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(regulatorRegistrationDecisionEvents.BuildMock());

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);

        var response = result.Value[0];
        response.AppReferenceNumber.Should().Be("APP123");
        response.Comments.Should().Be("Producer comment 1");
        response.Type.Should().Be("RegistrationApplicationSubmitted");
    }
}