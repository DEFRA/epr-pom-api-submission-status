namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionCreate;

using Application.Features.Commands.SubmissionCreate;
using Data.Entities.Submission;
using Data.Enums;
using Data.Repositories.Queries.Interfaces;
using FluentValidation.TestHelper;
using Moq;
using TestSupport;

[TestClass]
public class SubmissionCreateCommandValidatorTests
{
    private readonly Mock<IQueryRepository<Submission>> _mockQueryRepository;
    private readonly SubmissionCreateCommandValidator _systemUnderTest;

    public SubmissionCreateCommandValidatorTests()
    {
        _mockQueryRepository = new Mock<IQueryRepository<Submission>>();
        _systemUnderTest = new SubmissionCreateCommandValidator(_mockQueryRepository.Object);
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Validator_ReturnSuccess_WhenCommandValid(SubmissionType submissionType)
    {
        // Arrange
        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Validator_ReturnErrors_WhenSubmissionIdIsEmptyGuid(SubmissionType submissionType)
    {
        // Arrange
        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);
        command.Id = Guid.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("'Id' must not be empty.");
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Validator_ReturnErrors_WhenSubmissionDoesExists(SubmissionType submissionType)
    {
        // Arrange
        var id = Guid.NewGuid();
        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);
        command.Id = id;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission
            {
                Id = id
            });

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage($"Submission with id {id} does exist.");
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Validator_ReturnErrors_WhenOrganisationIdIsEmpty(SubmissionType submissionType)
    {
        // Arrange
        var id = Guid.NewGuid();
        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);
        command.OrganisationId = null;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission
            {
                Id = id
            });

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganisationId)
            .WithErrorMessage("'Organisation Id' must not be empty.");
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Validator_ReturnErrors_WhenOrganisationIdIsDefault(SubmissionType submissionType)
    {
        // Arrange
        var id = Guid.NewGuid();
        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);
        command.OrganisationId = Guid.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission
            {
                Id = id
            });

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganisationId)
            .WithErrorMessage($"'Organisation Id' must not be equal to '{Guid.Empty}'.");
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Validator_ReturnErrors_WhenUserIdIsEmpty(SubmissionType submissionType)
    {
        // Arrange
        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);
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
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Validator_ReturnErrors_WhenUserIdIsDefault(SubmissionType submissionType)
    {
        // Arrange
        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);
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
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Validator_ReturnErrors_WhenSubmissionPeriodIsEmpty(SubmissionType submissionType)
    {
        // Arrange
        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);
        command.SubmissionPeriod = string.Empty;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionPeriod)
            .WithErrorMessage("'Submission Period' must not be empty.");
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Validator_ReturnErrors_WhenSubmissionPeriodIsLessThanFour(SubmissionType submissionType)
    {
        // Arrange
        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);
        command.SubmissionPeriod = new string('1', 3);

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionPeriod)
            .WithErrorMessage($"The length of 'Submission Period' must be at least 4 characters. You entered {command.SubmissionPeriod.Length} characters.");
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Validator_ReturnErrors_WhenSubmissionTypeIsNotDefined(SubmissionType submissionType)
    {
        // Arrange
        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);
        command.SubmissionType = 0;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionType)
            .WithErrorMessage($"'Submission Type' has a range of values which does not include '0'.");
    }

    [TestMethod]
    [DataRow(SubmissionType.Producer)]
    [DataRow(SubmissionType.Registration)]
    public async Task Validator_ReturnErrors_WhenDataSourceTypeIsNotDefined(SubmissionType submissionType)
    {
        // Arrange
        var command = TestCommands.Submission.ValidSubmissionCreateCommand(submissionType);
        command.DataSourceType = 0;

        _mockQueryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Submission());

        // Act
        var result = await _systemUnderTest.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DataSourceType)
            .WithErrorMessage($"'Data Source Type' has a range of values which does not include '0'.");
    }
}