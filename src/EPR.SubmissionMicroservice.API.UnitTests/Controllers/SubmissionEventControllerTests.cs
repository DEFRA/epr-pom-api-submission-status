namespace EPR.SubmissionMicroservice.API.UnitTests.Controllers;

using System.Net;
using API.Controllers;
using API.Services.Interfaces;
using Application.Features.Commands.SubmissionEventCreate;
using Application.Features.Queries.SubmissionEventsGet;
using AutoMapper;
using Data.Enums;
using EPR.SubmissionMicroservice.API.Contracts.SubmissionEvents.Get;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using Errors;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json.Linq;
using TestSupport;

[TestClass]
public class SubmissionEventControllerTests
{
    private Mock<ISender> _mockMediator;
    private Mock<IHeaderSetter> _mockHeaderSetter;
    private Mock<IMapper> _mockMapper;

    private SubmissionEventController _systemUnderTest;

    [TestInitialize]
    public async Task Setup()
    {
        _mockMediator = new Mock<ISender>();
        _mockHeaderSetter = new Mock<IHeaderSetter>();
        _mockMapper = new Mock<IMapper>();

        var serviceProvider = new ServiceCollection()
            .AddScoped(_ => _mockMediator.Object)
            .BuildServiceProvider();

        _systemUnderTest = new SubmissionEventController(_mockHeaderSetter.Object, _mockMapper.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
            },
            ProblemDetailsFactory = new MockProblemDetailsFactory()
        };
    }

    [TestMethod]
    public async Task CreateSubmissionEvent_ReturnsCreated()
    {
        // Arrange
        const EventType eventType = EventType.ProducerValidation;

        var request = TestRequests.SubmissionEvent.ValidSubmissionEventCreateRequest(eventType);

        var command = TestCommands.SubmissionEvent.ValidSubmissionEventCreateCommand(eventType);

        _mockHeaderSetter.Setup(x => x.Set(It.IsAny<AbstractSubmissionEventCreateCommand>()))
            .Returns(command);

        _mockMediator
            .Setup(x => x.Send(It.IsAny<AbstractSubmissionEventCreateCommand>(), default))
            .ReturnsAsync(new SubmissionEventCreateResponse(command.SubmissionId));

        // Act
        var result = (IStatusCodeActionResult)await _systemUnderTest.CreateEvent(command.SubmissionId, request);

        // Assert
        result.StatusCode.Should().Be((int)HttpStatusCode.Created);
    }

    [TestMethod]
    public async Task CreateSubmissionEvent_NoType_ReturnsProblem()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        var request = new JObject();

        var expectedResult = "'Type' is required.";

        // Act
        var validationProblemDetails = await _systemUnderTest.CreateEvent(submissionId, request) as ObjectResult;
        var result = validationProblemDetails.Value.As<ValidationProblemDetails>().Errors["Type"][0];

        // Assert
        result.Should().Be(expectedResult);
    }

    [TestMethod]
    public async Task CreateSubmissionEvent_InvalidType_ReturnsProblem()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        var eventType = 100;
        var request = JObject.FromObject(new
        {
            type = eventType,
            fileName = "MyFile.csv",
            fileType = FileType.Pom
        });

        var expectedResult = $"'Type' has a range of values which does not include '{eventType}'.";

        // Act
        var validationProblemDetails = await _systemUnderTest.CreateEvent(submissionId, request) as ObjectResult;
        var result = validationProblemDetails.Value.As<ValidationProblemDetails>().Errors["Type"][0];

        // Assert
        result.Should().Be(expectedResult);
    }

    [TestMethod]
    public async Task CreateValidationEvent_ContainsValidationErrors_ReturnsCreated()
    {
        // Arrange
        var request = TestRequests.SubmissionEvent.ValidSubmissionEventCreateRequest(EventType.CheckSplitter);
        request["validationErrors"] = new JArray
        {
            new JObject
            {
                ["RowNumber"] = 1
            }
        };

        var command = TestCommands.SubmissionEvent.ValidSubmissionEventCreateCommand(EventType.CheckSplitter);

        _mockHeaderSetter.Setup(x => x.Set(It.IsAny<AbstractSubmissionEventCreateCommand>()))
            .Returns(command);

        _mockMediator
            .Setup(x => x.Send(It.IsAny<AbstractSubmissionEventCreateCommand>(), default))
            .ReturnsAsync(new SubmissionEventCreateResponse(command.SubmissionId));

        // Act
        var result = (IStatusCodeActionResult)await _systemUnderTest.CreateEvent(command.SubmissionId, request);

        // Assert
        result.StatusCode.Should().Be((int)HttpStatusCode.Created);
    }

    [TestMethod]
    public async Task GetSubmissions_ReturnsOkObjectResultWithSubmissions()
    {
        // Arrange
        var response = new List<AbstractSubmissionEventGetResponse>
        {
            new RegulatorDecisionGetResponse
            {
                FileId = Guid.NewGuid()
            },
            new RegulatorDecisionGetResponse
            {
                FileId = Guid.NewGuid()
            }
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<RegulatorPoMDecisionSubmissionEventsGetQuery>(), CancellationToken.None))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetSubmissionEventsByType(new RegulatorPoMDecisionSubmissionEventsGetRequest() { LastSyncTime = DateTime.Now }) as OkObjectResult;

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        result.Value.Should().Be(response);
    }

    [TestMethod]
    public async Task GetRegulatorRegistrationDecisionSubmissionEvents_ReturnsOkObjectResult()
    {
        // Arrange
        var request = new RegulatorRegistrationDecisionSubmissionEventsGetRequest();
        var query = new RegulatorRegistrationDecisionSubmissionEventsGetQuery();
        var response = new List<RegulatorRegistrationDecisionGetResponse>();

        _mockHeaderSetter.Setup(x => x.Set(It.IsAny<RegulatorRegistrationDecisionSubmissionEventsGetQuery>()))
            .Returns(query);

        _mockMediator
            .Setup(x => x.Send(query, CancellationToken.None))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetRegulatorRegistrationSubmissionEvents(request) as ObjectResult;

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().Be(response);
    }
}
