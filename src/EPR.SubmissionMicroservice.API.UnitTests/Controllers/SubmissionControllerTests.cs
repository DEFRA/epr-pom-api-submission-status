using EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;

namespace EPR.SubmissionMicroservice.API.UnitTests.Controllers;

using API.Controllers;
using API.Services.Interfaces;
using Application.Features.Commands.SubmissionCreate;
using Application.Features.Commands.SubmissionSubmit;
using Application.Features.Queries.Common;
using Application.Features.Queries.SubmissionFileGet;
using Application.Features.Queries.SubmissionGet;
using Application.Features.Queries.SubmissionOrganisationDetailsGet;
using Application.Features.Queries.SubmissionsEventsGet;
using Application.Features.Queries.SubmissionsGet;
using Application.Features.Queries.SubmissionsPeriodGet;
using AutoMapper;
using Contracts.Submission.Submit;
using Contracts.Submissions.Get;
using Data.Enums;
using EPR.SubmissionMicroservice.Application.Features.Queries.SubmissionUploadedFileGet;
using ErrorOr;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task GetSubmissionFiles_Return_OkObjectResult()
    {
        // Arrange
        var response = TestQueries.Submission.ValidSubmissionFileResponse();

        _mockMediator
            .Setup(x => x.Send(It.IsAny<SubmissionFileGetQuery>(), CancellationToken.None))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetSubmissionFile(new Guid()) as OkObjectResult;

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().Be(response);
        result.Value.Should().BeOfType<SubmissionFileGetResponse>();
    }

    [TestMethod]
    public async Task GetSubmissionOrganisationDetails_Return_OkObjectResult()
    {
        // Arrange
        var response = new SubmissionOrganisationDetailsGetResponse
        {
            BlobName = Guid.NewGuid().ToString(),
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<SubmissionOrganisationDetailsGetQuery>(), CancellationToken.None))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetSubmissionOrganisationDetails(new Guid(), "test_blob") as OkObjectResult;

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().Be(response);
        result.Value.Should().BeOfType<SubmissionOrganisationDetailsGetResponse>();
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

    [TestMethod]
    public async Task GetSubmissionsByType_Return_OkObjectResult()
    {
        // Arrange
        var response = new List<SubmissionGetResponse>
        {
            new()
            {
                SubmissionId = Guid.NewGuid(),
                SubmissionPeriod = "Jan to Jun 2023",
                Year = 2023
            }
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<SubmissionsPeriodGetQuery>(), CancellationToken.None))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetSubmissionsByType(new SubmissionGetRequest()) as OkObjectResult;

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().Be(response);
        result.Value.Should().BeOfType<List<SubmissionGetResponse>>();
    }

    [TestMethod]
    public async Task GetSubmissionsByType_ReturnEmptyArray_OkObjectResult()
    {
        // Arrange
        _mockMediator
            .Setup(x => x.Send(It.IsAny<SubmissionsPeriodGetQuery>(), CancellationToken.None))
            .ReturnsAsync(new List<SubmissionGetResponse>());

        // Act
        var result = await _systemUnderTest.GetSubmissionsByType(new SubmissionGetRequest()) as OkObjectResult;

        // Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().BeOfType<List<SubmissionGetResponse>>();
        result.Value.Should().BeEquivalentTo(Enumerable.Empty<SubmissionGetResponse>());
    }

    [TestMethod]
    public async Task GetSubmissionEvents_Return_OkObjectResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        string fileName = "TestFile.csv";

        var response = new SubmissionsEventsGetResponse();

        response.SubmittedEvents.Add(new SubmittedEvents
        {
            Created = DateTime.Now,
            FileId = fileId,
            FileName = fileName,
            SubmissionId = submissionId,
            SubmittedBy = "Test User",
            UserId = userId
        });

        response.RegulatorDecisionEvents.Add(new RegulatorDecisionEvents
        {
            Comment = string.Empty,
            Created = DateTime.Now,
            Decision = "Accepted",
            FileId = fileId,
            FileName = fileName,
            SubmissionId = submissionId,
            UserId = userId
        });

        response.AntivirusCheckEvents.Add(new AntivirusCheckEvents
        {
            UserId = userId,
            FileId = fileId,
            FileName = fileName,
            SubmissionId = submissionId,
            Created = DateTime.Now
        });

        _mockMediator
            .Setup(x => x.Send(It.IsAny<SubmissionsEventsGetQuery>(), CancellationToken.None))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetSubmissionEvents(submissionId, new FileSubmissionsEventGetRequest()) as OkObjectResult;

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().Be(response);
        result.Value.Should().BeOfType<SubmissionsEventsGetResponse>();
    }

    [TestMethod]
    public async Task GetSubmissionEvents_ReturnEmptyObject_OkObjectResult()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        _mockMediator
            .Setup(x => x.Send(It.IsAny<SubmissionsEventsGetQuery>(), CancellationToken.None))
            .ReturnsAsync(new SubmissionsEventsGetResponse());

        // Act
        var result = await _systemUnderTest.GetSubmissionEvents(submissionId, new FileSubmissionsEventGetRequest()) as OkObjectResult;

        // Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().BeOfType<SubmissionsEventsGetResponse>();
    }

    [TestMethod]
    public async Task GetRegistrationApplicationSubmissionDetails_ReturnEmptyObject_OkObjectResult()
    {
        // Arrange
        _mockMediator
            .Setup(x => x.Send(It.IsAny<GetRegistrationApplicationDetailsQuery>(), CancellationToken.None))
            .ReturnsAsync(ErrorOrFactory.From(new GetRegistrationApplicationDetailsResponse()));

        // Act
        var result = await _systemUnderTest.GetRegistrationApplicationDetails(new GetRegistrationApplicationDetailsRequest()) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [TestMethod]
    public async Task GetRegistrationApplicationSubmissionDetails_EmptyObject_NullResult()
    {
        // Arrange
        _mockMediator
            .Setup(x => x.Send(It.IsAny<GetRegistrationApplicationDetailsQuery>(), CancellationToken.None))
            .ReturnsAsync(new ErrorOr<GetRegistrationApplicationDetailsResponse>());

        // Act
        var result = await _systemUnderTest.GetRegistrationApplicationDetails(new GetRegistrationApplicationDetailsRequest()) as OkObjectResult;

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetSubmissionUploadedFile_Return_OkObjectResult()
    {
        // Arrange
        var response = TestQueries.Submission.ValidSubmissionUploadedFileResponse();

        _mockMediator
            .Setup(x => x.Send(It.IsAny<SubmissionUploadedFileGetQuery>(), CancellationToken.None))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetSubmissionUploadedFile(new Guid(), new Guid()) as OkObjectResult;

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        result.Value.Should().Be(response);
        result.Value.Should().BeOfType<SubmissionUploadedFileGetResponse>();
    }

    [TestMethod]
    public async Task GetPackagingDataResubmissionApplicationDetails_EmptyObject_NullResult()
    {
        // Arrange
        _mockMediator
            .Setup(x => x.Send(It.IsAny<GetPackagingResubmissionApplicationDetailsQuery>(), CancellationToken.None))
            .ReturnsAsync(new ErrorOr<List<GetPackagingResubmissionApplicationDetailsResponse>>());

        // Act
        var result = await _systemUnderTest.GetPackagingDataResubmissionApplicationDetails(new GetPackagingResubmissionApplicationDetailsRequest()) as OkObjectResult;

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetPackagingDataResubmissionApplicationDetails_ReturnEmptyObject_OkObjectResult()
    {
        // Arrange
        _mockMediator
            .Setup(x => x.Send(It.IsAny<GetPackagingResubmissionApplicationDetailsQuery>(), CancellationToken.None))
            .ReturnsAsync(ErrorOrFactory.From(new List<GetPackagingResubmissionApplicationDetailsResponse>()));

        // Act
        var result = await _systemUnderTest.GetPackagingDataResubmissionApplicationDetails(new GetPackagingResubmissionApplicationDetailsRequest()) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().NotBeNull();
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}