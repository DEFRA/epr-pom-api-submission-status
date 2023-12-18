using EPR.SubmissionMicroservice.API.Controllers;
using EPR.SubmissionMicroservice.Application;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventWarningGet;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TestSupport;
using Error = ErrorOr.Error;
using Guid = System.Guid;

namespace EPR.SubmissionMicroservice.API.UnitTests.Controllers;

[TestClass]
public class ValidationEventWarningControllerTests
{
    private Mock<ProblemDetailsFactory> _mockProblemDetailsFactory;
    private Mock<ISender> _mockMediator;

    private ValidationEventWarningController _systemUnderTest;

    [TestInitialize]
    public async Task Setup()
    {
        _mockProblemDetailsFactory = new Mock<ProblemDetailsFactory>();
        _mockMediator = new Mock<ISender>();

        var serviceProvider = new ServiceCollection()
            .AddScoped(_ => _mockMediator.Object)
            .AddScoped(_ => _mockProblemDetailsFactory.Object)
            .BuildServiceProvider();

        _systemUnderTest = new ValidationEventWarningController()
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = serviceProvider,
                    Items = new Dictionary<object, object> { { Constants.Http.Errors, new List<Error>() } }
                }
            }
        };
    }

    [TestMethod]
    public async Task Get_WhenRequestValid_ReturnsOk()
    {
        // Arrange
        var submissionId = It.IsAny<Guid>();
        var producerValidationGetResponse = new List<AbstractValidationIssueGetResponse>
        {
            TestQueries.ProducerValidation.ValidProducerValidationWarningGetResponse()
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<ValidationEventWarningGetQuery>(), default))
            .ReturnsAsync(producerValidationGetResponse);

        // Act
        var result = await _systemUnderTest.Get(submissionId) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().BeEquivalentTo(producerValidationGetResponse);
    }

    [TestMethod]
    public async Task Get_WhenRecordNotExists_ReturnsNotFound()
    {
        // Arrange
        var producerValidationId = Guid.NewGuid();
        var problemDetails = new ProblemDetails()
        {
            Status = 404
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<ValidationEventWarningGetQuery>(), default))
            .ReturnsAsync(Error.NotFound());

        _mockProblemDetailsFactory
            .Setup(x => x.CreateProblemDetails(
                It.IsAny<HttpContext>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(problemDetails);

        // Act
        var result = await _systemUnderTest.Get(producerValidationId) as ObjectResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [TestMethod]
    public async Task Validate_ProducerValidationWarningGetResponse()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var producerValidationGetResponses = new List<AbstractValidationIssueGetResponse>
        {
            TestQueries.ProducerValidation.ValidProducerValidationWarningGetResponse()
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<ValidationEventWarningGetQuery>(), default))
            .ReturnsAsync(producerValidationGetResponses);

        // Act
        var result = await _systemUnderTest.Get(submissionId) as OkObjectResult;

        // Assert
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<ProducerValidationIssueGetResponse>().DataSubmissionPeriod.Should().NotBeNull();
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<ProducerValidationIssueGetResponse>().SubsidiaryId.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Get_WhenUnexpectedErrorOccurs_ReturnsProblemDetails()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var problemDetails = new ProblemDetails()
        {
            Status = 500,
            Title = "Unexpected Error"
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<ValidationEventWarningGetQuery>(), default))
            .ReturnsAsync(Error.Failure());

        _mockProblemDetailsFactory
            .Setup(x => x.CreateProblemDetails(
                It.IsAny<HttpContext>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(problemDetails);

        // Act
        var result = await _systemUnderTest.Get(submissionId) as ObjectResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.Value.Should().BeEquivalentTo(problemDetails);
    }
}