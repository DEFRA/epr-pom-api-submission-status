using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.Helpers;

[TestClass]
public class CompaniesHouseSubmissionEventHelperTests
{
    private const string FileOneName = "file-one.csv";
    private const string FileOneBlobName = "blob-name-one";
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _fileId = Guid.NewGuid();
    private readonly DateTime _fileOneSubmittedDateTime = DateTime.Now;
    private readonly DateTime _fileCreatedDateTime = DateTime.Now;
    private readonly Guid _submissionId = Guid.NewGuid();
    private Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventsRepositoryMock;
    private Mock<IQueryRepository<AbstractValidationError>> _validationErrorRepositoryMock;
    private CompaniesHouseSubmissionGetResponse? _companiesHouseSubmissionGetResponse;
    private CompaniesHouseSubmissionEventHelper _systemUnderTest;

    [TestInitialize]
    public async Task TestInitialize()
    {
        _submissionEventsRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _companiesHouseSubmissionGetResponse = new CompaniesHouseSubmissionGetResponse { Id = _submissionId };
        _validationErrorRepositoryMock = new Mock<IQueryRepository<AbstractValidationError>>();

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>().BuildMock);

        _validationErrorRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
            .Returns(new List<AbstractValidationError>().BuildMock);

        _systemUnderTest = new CompaniesHouseSubmissionEventHelper(
            _submissionEventsRepositoryMock.Object);
    }

    [TestMethod]
    public async Task SetValidationEvents_DoesNotSetAnyProperties_WhenNoEventsExist()
    {
        // Act
        await _systemUnderTest.SetValidationEventsAsync(_companiesHouseSubmissionGetResponse, CancellationToken.None);

        // Assert
        _companiesHouseSubmissionGetResponse.Should().BeEquivalentTo(new CompaniesHouseSubmissionGetResponse
        {
            Id = _submissionId,
            CompaniesHouseFileName = null,
            CompaniesHouseFileUploadDateTime = null,
            CompaniesHouseDataComplete = false,
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
                Created = _fileCreatedDateTime,
                FileId = _fileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileId,
                AntivirusScanResult = AntivirusScanResult.Success
            }
        };

        var error = new ProducerValidationError();

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_companiesHouseSubmissionGetResponse, CancellationToken.None);

        // Assert
        _companiesHouseSubmissionGetResponse.Should().BeEquivalentTo(new CompaniesHouseSubmissionGetResponse
        {
            Id = _submissionId,
            CompaniesHouseFileName = FileOneName,
            CompaniesHouseFileUploadDateTime = _fileCreatedDateTime,
            CompaniesHouseDataComplete = true,
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
                Created = _fileCreatedDateTime,
                FileId = _fileId,
                UserId = _userId
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_companiesHouseSubmissionGetResponse, CancellationToken.None);

        // Assert
        _companiesHouseSubmissionGetResponse.Should().BeEquivalentTo(new CompaniesHouseSubmissionGetResponse
        {
            Id = _submissionId,
            CompaniesHouseFileName = FileOneName,
            CompaniesHouseFileUploadDateTime = _fileCreatedDateTime,
            CompaniesHouseDataComplete = false,
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
                Created = _fileCreatedDateTime,
                FileId = _fileId,
                UserId = _userId
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileId,
                Errors = errors
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_companiesHouseSubmissionGetResponse, CancellationToken.None);

        // Assert
        _companiesHouseSubmissionGetResponse.Should().BeEquivalentTo(new CompaniesHouseSubmissionGetResponse
        {
            Id = _submissionId,
            CompaniesHouseFileName = FileOneName,
            CompaniesHouseFileUploadDateTime = _fileCreatedDateTime,
            CompaniesHouseDataComplete = false,
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
                Created = _fileCreatedDateTime,
                FileId = _fileId,
                UserId = _userId,
                Errors = errors
            },
            new AntivirusResultEvent
            {
                SubmissionId = _submissionId,
                BlobName = FileOneBlobName,
                FileId = _fileId
            }
        };

        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        await _systemUnderTest.SetValidationEventsAsync(_companiesHouseSubmissionGetResponse, CancellationToken.None);

        // Assert
        _companiesHouseSubmissionGetResponse.Should().BeEquivalentTo(new CompaniesHouseSubmissionGetResponse
        {
            Id = _submissionId,
            CompaniesHouseFileName = FileOneName,
            CompaniesHouseFileUploadDateTime = _fileCreatedDateTime,
            CompaniesHouseDataComplete = false,
            ValidationPass = true,
            HasWarnings = false,
            Errors = errors
        });
    }
}