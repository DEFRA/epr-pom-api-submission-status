namespace EPR.SubmissionMicroservice.API.UnitTests.Controllers;

using API.Controllers;
using API.Services.Interfaces;
using Application.Features.Commands.SubmissionCreate;
using Application.Features.Commands.SubmissionSubmit;
using Application.Features.Queries.Common;
using Application.Features.Queries.SubmissionGet;
using Application.Features.Queries.SubmissionsGet;
using AutoMapper;
using Contracts.Submission.Submit;
using Contracts.Submissions.Get;
using Data.Enums;
using ErrorOr;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TestSupport;

[TestClass]
public class SubmissionControllerTests
{
    private readonly Mock<IHeaderSetter> _mockHeaderSetter;
    private readonly Mock<ISender> _mockMediator;

    private readonly SubmissionController _systemUnderTest;

    public SubmissionControllerTests()
    {
        _mockHeaderSetter = new Mock<IHeaderSetter>();
        _mockMediator = new Mock<ISender>();
        _systemUnderTest = new SubmissionController(Mock.Of<IMapper>(), _mockHeaderSetter.Object)
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
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task CreateSubmission_ReturnsCreated(SubmissionType submissionType)
    {
        // Arrange
        var request = TestRequests.Submission.ValidSubmissionCreateRequest(submissionType);

        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);

        _mockHeaderSetter.Setup(x => x.Set(It.IsAny<SubmissionCreateCommand>()))
            .Returns(command);

        _mockMediator
            .Setup(x => x.Send(It.IsAny<SubmissionCreateCommand>(), default))
            .ReturnsAsync(new SubmissionCreateResponse(command.Id));

        // Act
        var result = await _systemUnderTest.CreateSubmission(request) as CreatedAtRouteResult;

        // Assert
        result.Should().NotBeNull();
        result.RouteValues["submissionId"].Should().Be(command.Id);
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task GetSubmission_Returns_OK(SubmissionType submissionType)
    {
        // Arrange
        var response = TestQueries.Submission.ValidSubmissionResponse(submissionType);

        _mockMediator
            .Setup(x => x.Send(It.IsAny<SubmissionGetQuery>(), default))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetSubmission(Guid.NewGuid(), Guid.NewGuid()) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.As<AbstractSubmissionGetResponse>().Id.Should().Be(response.Id);
    }

    [TestMethod]
    public async Task GetSubmissions_ReturnsOkObjectResultWithSubmissions()
    {
        // Arrange
        var response = new List<AbstractSubmissionGetResponse>
        {
            new PomSubmissionGetResponse
            {
                Id = Guid.NewGuid()
            },
            new RegistrationSubmissionGetResponse
            {
                Id = Guid.NewGuid()
            }
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<SubmissionsGetQuery>(), CancellationToken.None))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetSubmissions(new SubmissionsGetRequest()) as OkObjectResult;

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        result.Value.Should().Be(response);
    }

    [TestMethod]
    public async Task Submit_ReturnsNoContent_WhenNoErrorOccurs()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var request = new SubmissionPayload
        {
            SubmittedBy = "Test Name",
            FileId = fileId
        };
        var command = new SubmissionSubmitCommand
        {
            SubmissionId = submissionId,
            UserId = Guid.NewGuid(),
            FileId = fileId
        };

        _mockHeaderSetter.Setup(x => x.Set(It.IsAny<SubmissionSubmitCommand>())).Returns(command);

        _mockMediator
            .Setup(x => x.Send(command, CancellationToken.None))
            .ReturnsAsync(new ErrorOr<Unit>());

        // Act
        var result = await _systemUnderTest.Submit(submissionId, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockMediator.Verify(x => x.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }
}