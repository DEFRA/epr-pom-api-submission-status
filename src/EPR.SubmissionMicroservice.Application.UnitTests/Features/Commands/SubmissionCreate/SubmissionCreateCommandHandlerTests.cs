namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionCreate;

using Application.Features.Commands.SubmissionCreate;
using AutoMapper;
using Data.Entities.Submission;
using Data.Enums;
using Data.Repositories.Commands.Interfaces;
using ErrorOr;
using FluentAssertions;
using Moq;
using TestSupport;
using TestSupport.Helpers;

[TestClass]
public class SubmissionCreateCommandHandlerTests
{
    private readonly Mock<ICommandRepository<Submission>> _mockCommandRepository = new();
    private readonly IMapper _mapper = AutoMapperHelpers.GetMapper();

    private readonly SubmissionCreateCommandHandler _systemUnderTest;

    public SubmissionCreateCommandHandlerTests()
    {
        _systemUnderTest = new SubmissionCreateCommandHandler(_mockCommandRepository.Object, _mapper);
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Handle_GivenValidRequest_ShouldReturnSubmissionId(SubmissionType submissionType)
    {
        // Arrange
        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);

        var submission = TestEntities.Submission.ValidSubmission(submissionType);
        submission.Id = command.Id;

        // Act
        var result = await _systemUnderTest.Handle(command, default);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.As<SubmissionCreateResponse>().Id.Should().Be(submission.Id);
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Handle_GivenRepositoryError_ShouldReturnError(SubmissionType submissionType)
    {
        // Arrange
        _mockCommandRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Handle(TestCommands.Submission.ValidSubmissionCreateCommand(submissionType), default);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }
}
