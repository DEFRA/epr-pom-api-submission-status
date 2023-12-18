using EPR.SubmissionMicroservice.API.Controllers;
using EPR.SubmissionMicroservice.Application;
using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventErrorGet;
using ErrorOr;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TestSupport;

namespace EPR.SubmissionMicroservice.API.UnitTests.Controllers;

[TestClass]
public class ValidationEventErrorControllerTests
{
    private Mock<ISender> _mockMediator;
    private Mock<ProblemDetailsFactory> _mockProblemDetailsFactory;

    private ValidationEventErrorController _systemUnderTest;

    [TestInitialize]
    public async Task Setup()
    {
        _mockMediator = new Mock<ISender>();
        _mockProblemDetailsFactory = new Mock<ProblemDetailsFactory>();

        var serviceProvider = new ServiceCollection()
            .AddScoped(_ => _mockMediator.Object)
            .AddScoped(_ => _mockProblemDetailsFactory.Object)
            .BuildServiceProvider();

        _systemUnderTest = new ValidationEventErrorController
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
        var submissionId = Guid.NewGuid();
        var producerValidationGetResponses = new List<AbstractValidationIssueGetResponse>
        {
            TestQueries.ProducerValidation.ValidProducerValidationErrorGetResponse()
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<ValidationEventErrorGetQuery>(), default))
            .ReturnsAsync(producerValidationGetResponses);

        // Act
        var result = await _systemUnderTest.Get(submissionId) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().BeEquivalentTo(producerValidationGetResponses);
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
            .Setup(x => x.Send(It.IsAny<ValidationEventErrorGetQuery>(), default))
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
    public async Task Validate_ProducerValidationErrorGetResponse()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var producerValidationGetResponses = new List<AbstractValidationIssueGetResponse>
        {
            TestQueries.ProducerValidation.ValidProducerValidationErrorGetResponse()
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<ValidationEventErrorGetQuery>(), default))
            .ReturnsAsync(producerValidationGetResponses);

        // Act
        var result = await _systemUnderTest.Get(submissionId) as OkObjectResult;

        // Assert
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<ProducerValidationIssueGetResponse>().DataSubmissionPeriod.Should().NotBeNull();
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<ProducerValidationIssueGetResponse>().SubsidiaryId.Should().NotBeNull();
    }
}