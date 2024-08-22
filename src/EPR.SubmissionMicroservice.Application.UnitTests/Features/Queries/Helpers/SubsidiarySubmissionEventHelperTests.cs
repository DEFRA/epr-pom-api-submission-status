using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.Helpers;

[TestClass]
public class SubsidiarySubmissionEventHelperTests
{
    private const string FileOneName = "file-one.csv";
    private const string FileTwoName = "file-two.csv";
    private const string FileOneBlobName = "blob-name-one";
    private const string FileTwoBlobName = "blob-name-two";
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _fileOneFileId = Guid.NewGuid();
    private readonly Guid _fileTwoFileId = Guid.NewGuid();
    private readonly DateTime _fileOneSubmittedDateTime = DateTime.Now;
    private readonly DateTime _fileOneCreatedDateTime = DateTime.Now;
    private readonly DateTime _fileTwoCreatedDateTime = DateTime.Now.AddMinutes(1);
    private readonly Guid _submissionId = Guid.NewGuid();
    private Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventsRepositoryMock;
    private Mock<IQueryRepository<AbstractValidationError>> _validationErrorRepositoryMock;
    private SubsidiarySubmissionGetResponse? _subsidiarySubmissionGetResponse;
    private SubsidiarySubmissionEventHelper _systemUnderTest;

    [TestInitialize]
    public async Task TestInitialize()
    {
        _submissionEventsRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _subsidiarySubmissionGetResponse = new SubsidiarySubmissionGetResponse { Id = _submissionId };
        _validationErrorRepositoryMock = new Mock<IQueryRepository<AbstractValidationError>>();

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>().BuildMock);

        _validationErrorRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
            .Returns(new List<AbstractValidationError>().BuildMock);

        _systemUnderTest = new SubsidiarySubmissionEventHelper(
            _submissionEventsRepositoryMock.Object);
    }

    [TestMethod]
    public async Task SetValidationEvents_DoesNotSetAnyProperties_WhenNoEventsExist()
    {
        // Act
        await _systemUnderTest.SetValidationEventsAsync(_subsidiarySubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _subsidiarySubmissionGetResponse.Should().BeEquivalentTo(new SubsidiarySubmissionGetResponse
        {
            Id = _submissionId,
            SubsidiaryFileName = null,
            SubsidiaryFileUploadDateTime = null,
            SubsidiaryDataComplete = false,
            ValidationPass = false,
            HasWarnings = false
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_ForAllEvents()
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
                FileId = _fileOneFileId,
                AntivirusScanResult = AntivirusScanResult.Success
            }
        };

        var error = new ProducerValidationError();

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_subsidiarySubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _subsidiarySubmissionGetResponse.Should().BeEquivalentTo(new SubsidiarySubmissionGetResponse
        {
            Id = _submissionId,
            SubsidiaryFileName = FileOneName,
            SubsidiaryFileUploadDateTime = _fileOneCreatedDateTime,
            SubsidiaryDataComplete = true,
            ValidationPass = true,
            HasWarnings = false
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenAntivirusResultEventsDoesNotExist()
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
        await _systemUnderTest.SetValidationEventsAsync(_subsidiarySubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _subsidiarySubmissionGetResponse.Should().BeEquivalentTo(new SubsidiarySubmissionGetResponse
        {
            Id = _submissionId,
            SubsidiaryFileName = FileOneName,
            SubsidiaryFileUploadDateTime = _fileOneCreatedDateTime,
            SubsidiaryDataComplete = false,
            ValidationPass = true,
            HasWarnings = false
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenAntivirusCheckEventHasErrors()
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
        await _systemUnderTest.SetValidationEventsAsync(_subsidiarySubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _subsidiarySubmissionGetResponse.Should().BeEquivalentTo(new SubsidiarySubmissionGetResponse
        {
            Id = _submissionId,
            SubsidiaryFileName = FileOneName,
            SubsidiaryFileUploadDateTime = _fileOneCreatedDateTime,
            SubsidiaryDataComplete = false,
            ValidationPass = true,
            HasWarnings = false,
            Errors = errors
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
                UserId = _userId,
                Errors = errors
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
        await _systemUnderTest.SetValidationEventsAsync(_subsidiarySubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _subsidiarySubmissionGetResponse.Should().BeEquivalentTo(new SubsidiarySubmissionGetResponse
        {
            Id = _submissionId,
            SubsidiaryFileName = FileOneName,
            SubsidiaryFileUploadDateTime = _fileOneCreatedDateTime,
            SubsidiaryDataComplete = false,
            ValidationPass = true,
            HasWarnings = false,
            Errors = errors
        });
    }
}