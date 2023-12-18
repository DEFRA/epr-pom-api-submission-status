namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.Helpers;

using System.Linq.Expressions;
using Application.Features.Queries.Common;
using Application.Features.Queries.Helpers;
using Data.Entities.AntivirusEvents;
using Data.Entities.SubmissionEvent;
using Data.Enums;
using Data.Repositories.Queries.Interfaces;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;

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
    private PomSubmissionGetResponse? _pomSubmissionGetResponse;
    private PomSubmissionEventHelper _systemUnderTest;

    [TestInitialize]
    public async Task TestInitialize()
    {
        _submissionEventsRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _pomSubmissionGetResponse = new PomSubmissionGetResponse { Id = _submissionId };
        _systemUnderTest = new PomSubmissionEventHelper(_submissionEventsRepositoryMock.Object);
    }

    [TestMethod]
    public async Task SetValidationEvents_DoesNotSetAnyProperties_WhenNoEventsExist()
    {
        // Arrange
        _submissionEventsRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>().BuildMock);

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
            ValidationPass = false
        });
    }

    [TestMethod]
    public async Task SetValidationEventsAsync_SetsPropertiesCorrectly_WhenAllEventsShowSuccess()
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
            },
            new SubmittedEvent
            {
                SubmissionId = _submissionId,
                FileId = _fileOneFileId,
                Created = _fileOneSubmittedDateTime,
                UserId = _userId
            },
            new RegulatorPoMDecisionEvent
            {
                Decision = RegulatorDecision.Rejected,
                Comments = Comments,
                IsResubmissionRequired = true
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
            LastUploadedValidFile = new UploadedPomFileInformation
            {
                FileName = FileOneName,
                FileUploadDateTime = _fileOneCreatedDateTime,
                UploadedBy = _userId,
                FileId = _fileOneFileId
            },
            LastSubmittedFile = new SubmittedPomFileInformation
            {
                FileId = _fileOneFileId,
                FileName = FileOneName,
                SubmittedBy = _userId,
                SubmittedDateTime = _fileOneSubmittedDateTime
            },
            PomDataComplete = true,
            ValidationPass = true
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
            ValidationPass = true
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