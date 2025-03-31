using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.Helpers;

[TestClass]
public class PomSubmissionEventHelperTests
{
    private const string FileOneName = "file-one.csv";
    private const string FileTwoName = "file-two.csv";
    private const string FileOneBlobName = "blob-name-one";
    private const string FileTwoBlobName = "blob-name-two";
    private const string Comments = "Invalid PoM data";
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _fileOneFileId = Guid.NewGuid();
    private readonly Guid _fileTwoFileId = Guid.NewGuid();
    private readonly DateTime _fileOneSubmittedDateTime = DateTime.Now;
    private readonly DateTime _fileOneCreatedDateTime = DateTime.Now;
    private readonly DateTime _fileTwoCreatedDateTime = DateTime.Now.AddMinutes(1);
    private readonly Guid _submissionId = Guid.NewGuid();
    private Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventsRepositoryMock;
    private Mock<IQueryRepository<AbstractValidationWarning>> _validationWarningRepositoryMock;
    private Mock<IQueryRepository<AbstractValidationError>> _validationErrorRepositoryMock;
    private PomSubmissionGetResponse? _pomSubmissionGetResponse;
    private PomSubmissionEventHelper _systemUnderTest;

    [TestInitialize]
    public async Task TestInitialize()
    {
        _submissionEventsRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _pomSubmissionGetResponse = new PomSubmissionGetResponse { Id = _submissionId };
        _validationWarningRepositoryMock = new Mock<IQueryRepository<AbstractValidationWarning>>();
        _validationErrorRepositoryMock = new Mock<IQueryRepository<AbstractValidationError>>();

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>().BuildMock);

        _validationErrorRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
            .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);

        _systemUnderTest = new PomSubmissionEventHelper(
            _submissionEventsRepositoryMock.Object,
            _validationWarningRepositoryMock.Object,
            _validationErrorRepositoryMock.Object);
    }

    [TestMethod]
    public async Task SetValidationEvents_DoesNotSetAnyProperties_WhenNoEventsExist()
    {
        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = null,
            PomFileUploadDateTime = null,
            LastUploadedValidFile = null,
            LastSubmittedFile = null,
            PomDataComplete = false,
            ValidationPass = false,
            HasWarnings = false
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_ForAllEvents()
    {
        // Arrange
        var validFile = "ValidFile";
        var validFileBlobName = Guid.NewGuid().ToString();
        var validFileCreatedDateTime = DateTime.Now.AddHours(-5);
        var validFileFileId = Guid.NewGuid();

        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = validFile,
                Created = validFileCreatedDateTime,
                FileId = validFileFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = validFileBlobName,
                FileId = validFileFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 2,
                Created = DateTime.Now
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = validFileBlobName,
                DataCount = 1,
                Created = validFileCreatedDateTime
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = false,
                ErrorCount = 2,
                WarningCount = 2,
                Created = _fileOneCreatedDateTime
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = false,
                ErrorCount = 1,
                WarningCount = 1,
                Created = _fileOneCreatedDateTime
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = validFileBlobName,
                IsValid = true,
                Created = validFileCreatedDateTime
            },
            new SubmittedEvent
            {
                SubmissionId = _submissionId,
                FileId = validFileFileId,
                Created = validFileCreatedDateTime,
                UserId = _userId
            },
            new RegulatorPoMDecisionEvent
            {
                Decision = RegulatorDecision.Rejected,
                Comments = Comments,
                IsResubmissionRequired = true
            },
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = _submissionId,
                Created = _fileOneCreatedDateTime.AddMinutes(-2)
            }
        };

        var error = new ProducerValidationError();

        var warning = new ProducerValidationWarning();

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        _validationErrorRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
            .Returns(new List<AbstractValidationError>
            {
                error,
                error,
                error
            }.BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>
            {
                warning,
                warning,
                warning
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileOneName,
            PomFileUploadDateTime = _fileOneCreatedDateTime,
            LastUploadedValidFile = new UploadedPomFileInformation
            {
                FileName = validFile,
                FileUploadDateTime = validFileCreatedDateTime,
                UploadedBy = _userId,
                FileId = validFileFileId
            },
            LastSubmittedFile = new SubmittedPomFileInformation
            {
                FileId = validFileFileId,
                FileName = validFile,
                SubmittedBy = _userId,
                SubmittedDateTime = validFileCreatedDateTime
            },
            PomDataComplete = true,
            ValidationPass = false,
            HasWarnings = true,
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_ThrowsException_WhenAntivirusCheckEventMatchingSubmittedFileIdIsNotFound()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1,
                Created = DateTime.Now
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = true
            },
            new SubmittedEvent
            {
                SubmissionId = _submissionId,
                FileId = _fileOneFileId,
                Created = _fileOneSubmittedDateTime,
                UserId = _userId
            },
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileTwoName,
                Created = _fileTwoCreatedDateTime,
                FileId = _fileTwoFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                FileId = _fileTwoFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                DataCount = 1,
                Created = DateTime.Now.AddHours(1)
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                IsValid = true
            },
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act / Assert
        await _systemUnderTest
            .Invoking(x => x.SetValidationEventsAsync(_pomSubmissionGetResponse, true, CancellationToken.None))
            .Should()
            .ThrowAsync<Exception>();
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_ThrowsException_WhenSubmittedEventIsNotFoundButIsSubmittedFlagIsTrue()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1,
                Created = DateTime.Now
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = true
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act / Assert
        await _systemUnderTest
            .Invoking(x => x.SetValidationEventsAsync(_pomSubmissionGetResponse, true, CancellationToken.None))
            .Should()
            .ThrowAsync<Exception>();
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenLatestValidFileIsNotTheSameAsTheLatestSubmittedFile()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileTwoName,
                Created = _fileTwoCreatedDateTime,
                FileId = _fileTwoFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                FileId = _fileTwoFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1,
                Created = DateTime.Now.AddHours(-2)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                DataCount = 1,
                Created = DateTime.Now
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = true
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                IsValid = true
            },
            new SubmittedEvent
            {
                SubmissionId = _submissionId,
                FileId = _fileOneFileId,
                Created = _fileOneSubmittedDateTime,
                UserId = _userId
            },
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = _submissionId,
                Created = _fileOneCreatedDateTime.AddMinutes(-2)
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileTwoName,
            PomFileUploadDateTime = _fileTwoCreatedDateTime,
            LastUploadedValidFile = new UploadedPomFileInformation
            {
                FileName = FileTwoName,
                FileUploadDateTime = _fileTwoCreatedDateTime,
                UploadedBy = _userId,
                FileId = _fileTwoFileId
            },
            LastSubmittedFile = new SubmittedPomFileInformation
            {
                FileId = _fileOneFileId,
                FileName = FileOneName,
                SubmittedBy = _userId,
                SubmittedDateTime = _fileOneSubmittedDateTime
            },
            PomDataComplete = true,
            ValidationPass = true,
            HasWarnings = false,
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenLatestValidFileIsNotTheSameAsTheLatestSubmittedFile_AppReferenceNumber_NotNull()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileTwoName,
                Created = _fileTwoCreatedDateTime,
                FileId = _fileTwoFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                FileId = _fileTwoFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1,
                Created = DateTime.Now.AddHours(-2)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                DataCount = 1,
                Created = DateTime.Now
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = true
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                IsValid = true
            },
            new SubmittedEvent
            {
                SubmissionId = _submissionId,
                FileId = _fileOneFileId,
                Created = _fileOneSubmittedDateTime,
                UserId = _userId
            },
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = _submissionId,
                Created = _fileOneCreatedDateTime.AddMinutes(-2)
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileTwoName,
            PomFileUploadDateTime = _fileTwoCreatedDateTime,
            LastUploadedValidFile = new UploadedPomFileInformation
            {
                FileName = FileTwoName,
                FileUploadDateTime = _fileTwoCreatedDateTime,
                UploadedBy = _userId,
                FileId = _fileTwoFileId
            },
            LastSubmittedFile = new SubmittedPomFileInformation
            {
                FileId = _fileOneFileId,
                FileName = FileOneName,
                SubmittedBy = _userId,
                SubmittedDateTime = _fileOneSubmittedDateTime
            },
            PomDataComplete = true,
            ValidationPass = true,
            HasWarnings = false,
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenLatestValidFileIsNotTheSameAsTheLatestSubmittedFile_AppReferenceNumberExists_SubmitEventCreated()
    {
        // Arrange
        var submittedEventTime = DateTime.Now.AddMinutes(2);
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileTwoName,
                Created = _fileTwoCreatedDateTime,
                FileId = _fileTwoFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                FileId = _fileTwoFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1,
                Created = DateTime.Now.AddHours(-2)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                DataCount = 1,
                Created = DateTime.Now
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = true
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                IsValid = true
            },
            new SubmittedEvent
            {
                SubmissionId = _submissionId,
                FileId = _fileOneFileId,
                Created = submittedEventTime,
                UserId = _userId
            },
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = _submissionId,
                Created = submittedEventTime.AddMinutes(-2)
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileTwoName,
            PomFileUploadDateTime = _fileTwoCreatedDateTime,
            LastUploadedValidFile = new UploadedPomFileInformation
            {
                FileName = FileTwoName,
                FileUploadDateTime = _fileTwoCreatedDateTime,
                UploadedBy = _userId,
                FileId = _fileTwoFileId
            },
            LastSubmittedFile = new SubmittedPomFileInformation
            {
                FileId = _fileOneFileId,
                FileName = FileOneName,
                SubmittedBy = _userId,
                SubmittedDateTime = submittedEventTime
            },
            PomDataComplete = true,
            ValidationPass = true,
            HasWarnings = false,
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenLatestValidFileIsNotTheSameAsTheLatestSubmittedFile_AppReferenceNumberExists_SubmitEventCreated_SubmittedResponse_NotNull()
    {
        // Arrange
        var submittedEventTime = DateTime.Now.AddMinutes(2);
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileTwoName,
                Created = _fileTwoCreatedDateTime,
                FileId = _fileTwoFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                FileId = _fileTwoFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1,
                Created = DateTime.Now.AddHours(-2)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                DataCount = 1,
                Created = DateTime.Now
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = true
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                IsValid = true
            },
            new SubmittedEvent
            {
                SubmissionId = _submissionId,
                FileId = _fileOneFileId,
                Created = submittedEventTime,
                UserId = _userId
            },
            new PackagingResubmissionApplicationSubmittedCreatedEvent
            {
                SubmissionId = _submissionId,
                Created = submittedEventTime.AddMinutes(-1),
            },
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = _submissionId,
                Created = submittedEventTime.AddMinutes(-2)
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileTwoName,
            PomFileUploadDateTime = _fileTwoCreatedDateTime,
            LastUploadedValidFile = new UploadedPomFileInformation
            {
                FileName = FileTwoName,
                FileUploadDateTime = _fileTwoCreatedDateTime,
                UploadedBy = _userId,
                FileId = _fileTwoFileId
            },
            LastSubmittedFile = new SubmittedPomFileInformation
            {
                FileId = _fileOneFileId,
                FileName = FileOneName,
                SubmittedBy = _userId,
                SubmittedDateTime = submittedEventTime
            },
            PomDataComplete = true,
            ValidationPass = true,
            HasWarnings = false,
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenLatestValidFileIsNotTheSameAsTheLatestSubmittedFile_AppReferenceNumberExists_SubmitEventCreated_SubmittedResponseCreated_NotLessThan_SubmittedEvent()
    {
        // Arrange
        var submittedEventTime = DateTime.Now.AddMinutes(2);
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileTwoName,
                Created = _fileTwoCreatedDateTime,
                FileId = _fileTwoFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                FileId = _fileTwoFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1,
                Created = DateTime.Now.AddHours(-2)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                DataCount = 1,
                Created = DateTime.Now
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = true
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                IsValid = true
            },
            new SubmittedEvent
            {
                SubmissionId = _submissionId,
                FileId = _fileOneFileId,
                Created = submittedEventTime,
                UserId = _userId
            },
            new PackagingResubmissionApplicationSubmittedCreatedEvent
            {
                SubmissionId = _submissionId,
                Created = submittedEventTime,
            },
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = _submissionId,
                Created = submittedEventTime.AddMinutes(-2)
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileTwoName,
            PomFileUploadDateTime = _fileTwoCreatedDateTime,
            LastUploadedValidFile = new UploadedPomFileInformation
            {
                FileName = FileTwoName,
                FileUploadDateTime = _fileTwoCreatedDateTime,
                UploadedBy = _userId,
                FileId = _fileTwoFileId
            },
            LastSubmittedFile = new SubmittedPomFileInformation
            {
                FileId = _fileOneFileId,
                FileName = FileOneName,
                SubmittedBy = _userId,
                SubmittedDateTime = submittedEventTime
            },
            PomDataComplete = true,
            ValidationPass = true,
            HasWarnings = false,
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenAntiVirusResultIsMissing()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1,
                Created = DateTime.Now.AddHours(-2)
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = true
            },
            new SubmittedEvent
            {
                SubmissionId = _submissionId,
                FileId = _fileOneFileId,
                Created = _fileOneSubmittedDateTime,
                UserId = _userId
            },
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = _submissionId,
                Created = _fileOneCreatedDateTime.AddMinutes(-2)
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileOneName,
            PomFileUploadDateTime = _fileOneCreatedDateTime,
            LastUploadedValidFile = null,
            LastSubmittedFile = new SubmittedPomFileInformation
            {
                FileId = _fileOneFileId,
                FileName = FileOneName,
                SubmittedBy = _userId,
                SubmittedDateTime = _fileOneSubmittedDateTime
            },
            PomDataComplete = false,
            ValidationPass = false,
            HasWarnings = false,
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenLatestAntivirusResultEventsDoesNotExist()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileOneName,
            PomFileUploadDateTime = _fileOneCreatedDateTime,
            PomDataComplete = false,
            ValidationPass = false,
            HasWarnings = false,
            LastUploadedValidFile = null,
            LastSubmittedFile = null
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenLatestCheckSplitterEventDoesntExist()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileOneName,
            PomFileUploadDateTime = _fileOneCreatedDateTime,
            PomDataComplete = false,
            ValidationPass = false,
            HasWarnings = false,
            LastUploadedValidFile = null,
            LastSubmittedFile = null
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenLatestProducerValidationEventsDoNotExist()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1,
                Created = DateTime.Now
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileOneName,
            PomFileUploadDateTime = _fileOneCreatedDateTime,
            PomDataComplete = false,
            ValidationPass = false,
            HasWarnings = false,
            LastUploadedValidFile = null,
            LastSubmittedFile = null
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenLatestUploadIsInvalidButAPreviousWasValid()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileTwoName,
                Created = _fileTwoCreatedDateTime,
                FileId = _fileTwoFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                FileId = _fileTwoFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1,
                Created = DateTime.Now.AddHours(-2)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                DataCount = 1,
                Created = DateTime.Now
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = true
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                IsValid = false
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileTwoName,
            PomFileUploadDateTime = _fileTwoCreatedDateTime,
            PomDataComplete = true,
            ValidationPass = false,
            HasWarnings = false,
            LastUploadedValidFile = new UploadedPomFileInformation
            {
                FileName = FileOneName,
                FileUploadDateTime = _fileOneCreatedDateTime,
                UploadedBy = _userId,
                FileId = _fileOneFileId
            },
            LastSubmittedFile = null
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenAntivirusResultEventHasErrors()
    {
        // Arrange
        var errors = new List<string> { "99" };
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId,
                Errors = errors
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileOneName,
            PomFileUploadDateTime = _fileOneCreatedDateTime,
            PomDataComplete = false,
            ValidationPass = false,
            HasWarnings = false,
            LastUploadedValidFile = null,
            LastSubmittedFile = null,
            Errors = errors
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenCheckSplitterEventHasErrors()
    {
        // Arrange
        var errors = new List<string> { "99" };
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1,
                Created = DateTime.Now,
                Errors = errors
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileOneName,
            PomFileUploadDateTime = _fileOneCreatedDateTime,
            PomDataComplete = false,
            ValidationPass = false,
            HasWarnings = false,
            LastUploadedValidFile = null,
            LastSubmittedFile = null,
            Errors = errors
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenProducerValidationEventHasErrors()
    {
        // Arrange
        var errors = new List<string> { "99" };
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1,
                Created = DateTime.Now
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = false,
                Errors = errors
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileOneName,
            PomFileUploadDateTime = _fileOneCreatedDateTime,
            PomDataComplete = true,
            ValidationPass = false,
            HasWarnings = false,
            LastUploadedValidFile = null,
            LastSubmittedFile = null,
            Errors = errors
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenSecondLatestUploadDataCountIsZero()
    {
        // Arrange
        var errors = new List<string> { "82" };
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileOneName,
                Created = _fileOneCreatedDateTime,
                FileId = _fileOneFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 0,
                Created = DateTime.Now,
                Errors = errors
            },
            new AntivirusCheckEvent
            {
                SubmissionId = _submissionId,
                FileName = FileTwoName,
                Created = _fileTwoCreatedDateTime,
                FileId = _fileTwoFileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                FileId = _fileTwoFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileTwoBlobName,
                DataCount = 1,
                Created = DateTime.Now
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = false,
                Errors = errors
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_pomSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _pomSubmissionGetResponse.Should().BeEquivalentTo(new PomSubmissionGetResponse
        {
            Id = _submissionId,
            PomFileName = FileTwoName,
            PomFileUploadDateTime = _fileTwoCreatedDateTime,
            PomDataComplete = false,
            ValidationPass = false,
            HasWarnings = false,
            LastUploadedValidFile = null,
            LastSubmittedFile = null
        });
    }

    [TestMethod]
    public async Task VerifyFileIdIsForValidFileAsync_ReturnsFalse_WhenNoMatchingAntivirusResultEventExists()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.VerifyFileIdIsForValidFileAsync(_submissionId, _fileTwoFileId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task VerifyFileIdIsForValidFileAsync_ReturnsFalse_WhenNoMatchingCheckSplitterEventExists()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.VerifyFileIdIsForValidFileAsync(_submissionId, _fileOneFileId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task VerifyFileIdIsForValidFileAsync_ReturnsFalse_WhenNotAllProducerValidationEventsAreValid()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 2
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = true
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = false
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.VerifyFileIdIsForValidFileAsync(_submissionId, _fileOneFileId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task VerifyFileIdIsForValidFileAsync_ReturnsFalse_WhenNotAllProducerValidationEventsAreFound()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 2
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = true
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.VerifyFileIdIsForValidFileAsync(_submissionId, _fileOneFileId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task VerifyFileIdIsForValidFileAsync_ReturnsTrue_WhenEventsForFileIdShowFileIsValid()
    {
        // Arrange
        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileOneFileId
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                DataCount = 1
            },
            new ProducerValidationEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                IsValid = true
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _systemUnderTest.VerifyFileIdIsForValidFileAsync(_submissionId, _fileOneFileId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }
}