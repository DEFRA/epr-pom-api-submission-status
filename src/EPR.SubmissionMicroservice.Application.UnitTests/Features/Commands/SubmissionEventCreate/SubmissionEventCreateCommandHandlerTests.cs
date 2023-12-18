using EPR.Common.Logging.Models;
using EPR.Common.Logging.Services;
using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionEventCreate;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Commands.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionEventCreate;

[TestClass]
public class SubmissionEventCreateCommandHandlerTests
{
    private readonly Mock<ICommandRepository<AbstractSubmissionEvent>> _mockCommandRepository = new();
    private readonly IMapper _mapper = AutoMapperHelpers.GetMapper();
    private readonly Mock<ILogger<SubmissionEventCreateCommandHandler>> _mockLogger = new();
    private readonly Mock<ILoggingService> _loggingService = new();

    private readonly SubmissionEventCreateCommandHandler _systemUnderTest;

    public SubmissionEventCreateCommandHandlerTests()
    {
        _systemUnderTest = new SubmissionEventCreateCommandHandler(
            _mockCommandRepository.Object,
            _loggingService.Object,
            _mapper,
            _mockLogger.Object);
    }

    [TestMethod]
    public async Task AntivirusCheckHandle_GivenValidCommand_ShouldReturnSuccess()
    {
        var antivirusEvent = TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand();

        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Handle(
            antivirusEvent,
            CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
    }

    [TestMethod]
    public async Task AntivirusCheckHandle_GivenRepositoryError_ShouldReturnError()
    {
        // Arrange
        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Handle(
            TestCommands.SubmissionEvent.ValidAntivirusCheckEventCreateCommand(),
            default);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }

    [TestMethod]
    public async Task AntivirusResultHandle_GivenValidCommand_ShouldReturnSuccess()
    {
        var antivirusEvent = TestCommands.SubmissionEvent.ValidAntivirusResultEventCreateCommand();

        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Handle(
            antivirusEvent,
            CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
    }

    [TestMethod]
    public async Task AntivirusResultHandle_GivenRepositoryError_ShouldReturnError()
    {
        // Arrange
        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Handle(
            TestCommands.SubmissionEvent.ValidAntivirusResultEventCreateCommand(),
            default);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }

    [TestMethod]
    public async Task CheckSplitterUploadHandle_GivenValidCommand_ShouldReturnSuccess()
    {
        var submissionEvent = TestCommands.SubmissionEvent.ValidCheckSplitterValidationEventCreateCommand();
        submissionEvent.Errors = new List<string>
        {
            "99"
        };
        submissionEvent.ValidationErrors = new List<AbstractValidationEventCreateCommand.AbstractValidationError>
        {
            new CheckSplitterValidationEventCreateCommand.CheckSplitterValidationError
            {
                ValidationErrorType = ValidationType.CheckSplitter,
                RowNumber = 1
            }
        };

        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Handle(
            submissionEvent,
            CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
    }

    [TestMethod]
    public async Task CheckSplitterHandle_GivenRepositoryError_ShouldReturnError()
    {
        // Arrange
        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Handle(
            TestCommands.SubmissionEvent.ValidCheckSplitterValidationEventCreateCommand(),
            default);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }

    [TestMethod]
    public async Task ProducerValidationUploadHandle_GivenValidCommand_ShouldReturnSuccess()
    {
        var submissionEvent = TestCommands.SubmissionEvent.ValidProducerValidationEventCreateCommand();
        submissionEvent.Errors = new List<string>
        {
            "99"
        };
        submissionEvent.ValidationErrors = new List<AbstractValidationEventCreateCommand.AbstractValidationError>
        {
            new ProducerValidationEventCreateCommand.ProducerValidationError
            {
                ValidationErrorType = ValidationType.ProducerValidation,
                RowNumber = 1,
                ErrorCodes = new List<string>
                {
                    "21"
                }
            }
        };

        submissionEvent.ValidationWarnings = new List<AbstractValidationEventCreateCommand.AbstractValidationWarning>
        {
            new ProducerValidationEventCreateCommand.ProducerValidationWarning
            {
                ValidationWarningType = ValidationType.ProducerValidation,
                RowNumber = 1,
                ErrorCodes = new List<string>
                {
                    "59"
                }
            }
        };

        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Handle(
            submissionEvent,
            CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
    }

    [TestMethod]
    public async Task ProducerValidationUploadHandle_GivenValidationErrors_ShouldLogToProtectiveMonitoring()
    {
        var submissionEvent = TestCommands.SubmissionEvent.ValidProducerValidationEventCreateCommand();

        submissionEvent.ValidationErrors = new List<AbstractValidationEventCreateCommand.AbstractValidationError>
        {
            new ProducerValidationEventCreateCommand.ProducerValidationError
            {
                ValidationErrorType = ValidationType.ProducerValidation,
                RowNumber = 1,
                ErrorCodes = new List<string>
                {
                    "10"
                }
            }
        };

        submissionEvent.ValidationWarnings = new List<AbstractValidationEventCreateCommand.AbstractValidationWarning>
        {
            new ProducerValidationEventCreateCommand.ProducerValidationWarning
            {
                ValidationWarningType = ValidationType.ProducerValidation,
                RowNumber = 1,
                ErrorCodes = new List<string>
                {
                    "10"
                }
            }
        };

        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(true);

        // Act
        await _systemUnderTest.Handle(
            submissionEvent,
            CancellationToken.None);

        // Assert
        _loggingService.Verify(
            x => x.SendEventAsync(
                It.IsAny<Guid>(),
                It.IsAny<ProtectiveMonitoringEvent>()),
            Times.Exactly(1));
    }

    [TestMethod]
    public async Task ProducerValidationUploadHandle_GivenNoValidationErrors_ShouldNotLogToProtectiveMonitoring()
    {
        var submissionEvent = TestCommands.SubmissionEvent.ValidProducerValidationEventCreateCommand();
        submissionEvent.ValidationErrors = new List<AbstractValidationEventCreateCommand.AbstractValidationError>();
        submissionEvent.ValidationWarnings = new List<AbstractValidationEventCreateCommand.AbstractValidationWarning>();

        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(true);

        // Act
        await _systemUnderTest.Handle(
            submissionEvent,
            CancellationToken.None);

        // Assert
        _loggingService.Verify(
            x => x.SendEventAsync(
                It.IsAny<Guid>(),
                It.IsAny<ProtectiveMonitoringEvent>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProducerValidationHandle_GivenRepositoryError_ShouldReturnError()
    {
        // Arrange
        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Handle(
            TestCommands.SubmissionEvent.ValidProducerValidationEventCreateCommand(),
            default);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }

    [TestMethod]
    public async Task RegistrationUploadHandle_GivenValidCommand_ShouldReturnSuccess()
    {
        var submissionEvent = TestCommands.SubmissionEvent.ValidRegistrationValidationEventCreateCommand();
        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(true);

        // Act
        var result = await _systemUnderTest.Handle(
            submissionEvent,
            CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
    }

    [TestMethod]
    public async Task RegistrationHandle_GivenRepositoryError_ShouldReturnError()
    {
        // Arrange
        _mockCommandRepository
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(false);

        // Act
        var result = await _systemUnderTest.Handle(
            TestCommands.SubmissionEvent.ValidRegistrationValidationEventCreateCommand(),
            default);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
    }
}