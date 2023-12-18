﻿namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate.Validators;

using Application.Features.Commands.SubmissionEventCreate.Validators;
using Data.Entities.Submission;
using Data.Repositories.Queries.Interfaces;
using FluentValidation.TestHelper;
using Moq;
using TestSupport;

[TestClass]
public class CheckSplitterValidationEventCreateCommandValidatorTests
{
    private readonly Mock<IQueryRepository<Submission>> _mockQueryRepository;
    private readonly CheckSplitterValidationEventCreateCommandValidator _systemUnderTest;

    public CheckSplitterValidationEventCreateCommandValidatorTests()
    {
        _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
        _systemUnderTest = new CheckSplitterValidationEventCreateCommandValidator(_mockQueryRepository.Object);
    }

    [TestMethod]
    public async Task Validator_ReturnSuccess_WhenCommandValid()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidCheckSplitterValidationEventCreateCommand();

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenSubmissionIdIsEmptyGuid()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidCheckSplitterValidationEventCreateCommand();
        command.SubmissionId = Guid.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionId)
            .WithErrorMessage($"'Submission Id' must not be equal to '{Guid.Empty}'.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenSubmissionDoesNotExist()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidCheckSplitterValidationEventCreateCommand();

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as Submission);

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionId)
            .WithErrorMessage($"Submission with id {command.SubmissionId} does not exist.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenUserIdIsEmpty()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidCheckSplitterValidationEventCreateCommand();
        command.UserId = null;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("'User Id' must not be empty.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenUserIdIsDefault()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidCheckSplitterValidationEventCreateCommand();
        command.UserId = Guid.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage($"'User Id' must not be equal to '{Guid.Empty}'.");
    }

    [TestMethod]
    public async Task Validator_ReturnErrors_WhenDataCountLessThanZero()
    {
        // Arrange
        var command = TestCommands.SubmissionEvent.ValidCheckSplitterValidationEventCreateCommand();
        command.DataCount = -1;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DataCount)
            .WithErrorMessage("'Data Count' must be greater than or equal to '0'.");
    }
}