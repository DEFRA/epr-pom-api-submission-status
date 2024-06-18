namespace EPR.SubmissionMicroservice.API.UnitTests.Controllers;

using API.Controllers;
using API.Services.Interfaces;
using Application.Features.Queries.Common;
using AutoMapper;
using EPR.SubmissionMicroservice.API.Contracts.Decisions.Get;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionEventsGet;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;

[TestClass]
public class DecisionControllerTests
{
    private readonly Mock<ISender> _mockMediator;

    private readonly DecisionController _systemUnderTest;

    public DecisionControllerTests()
    {
        Mock<IHeaderSetter> mockHeaderSetter = new Mock<IHeaderSetter>();
        _mockMediator = new Mock<ISender>();
        _systemUnderTest = new DecisionController(Mock.Of<IMapper>(), mockHeaderSetter.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = new ServiceCollection()
                        .AddScoped(_ => _mockMediator.Object)
                        .BuildServiceProvider()
                }
            }
        };
    }

    [TestMethod]
    public async Task GetDecision_ReturnsOkObjectResult()
    {
        // Arrange
        var request = new DecisionGetRequest
        {
            SubmissionId = Guid.NewGuid(),
            Limit = 1
        };

        var response = new List<AbstractSubmissionEventGetResponse>
        {
            new RegulatorDecisionGetResponse()
            {
                FileId = Guid.NewGuid(),
                Comments = "Test Comment 1",
                Decision = "Approved",
                IsResubmissionRequired = false,
                Created = DateTime.Today
            },
            new RegulatorDecisionGetResponse
            {
                FileId = Guid.NewGuid(),
                Comments = "Test Comment 2",
                Decision = "Rejected",
                IsResubmissionRequired = true,
                Created = DateTime.Today.AddDays(-1)
            }
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<RegulatorPoMDecisionSubmissionEventsGetQuery>(), CancellationToken.None))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetDecisions(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OkObjectResult>();
        var resultValue = (result as OkObjectResult).Value;
        resultValue.Should().Be(response);
    }

    [TestMethod]
    public async Task GetDecision_ReturnsErrorObjectResult()
    {
        // Arrange
        var request = new DecisionGetRequest
        {
            SubmissionId = Guid.NewGuid()
        };

        var responseErrors = new List<string>();

        var response = new List<AbstractSubmissionEventGetResponse>
        {
            new RegulatorDecisionGetResponse()
            {
                Errors = responseErrors
            }
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<RegulatorPoMDecisionSubmissionEventsGetQuery>(), CancellationToken.None))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetDecisions(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OkObjectResult>();
        var resultValue = (result as OkObjectResult).Value;
        resultValue.Should().Be(response);
    }

    [TestMethod]
    [ExpectedException(typeof(Exception))]
    public async Task GetDecision_ReturnsExceptionObjectResult()
    {
        // Arrange
        var request = new DecisionGetRequest
        {
            SubmissionId = Guid.NewGuid()
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<RegulatorPoMDecisionSubmissionEventsGetQuery>(), CancellationToken.None))
            .Throws(new Exception("Test Exception"));

        // Act
        var result = await _systemUnderTest.GetDecisions(request);

        // Assert
        result.Should().BeNull();
    }
}