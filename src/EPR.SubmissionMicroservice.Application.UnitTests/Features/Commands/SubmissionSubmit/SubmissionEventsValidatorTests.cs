namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Commands.SubmissionSubmit;

using EPR.SubmissionMicroservice.Application.Features.Commands.SubmissionSubmit;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

[TestClass]
public class SubmissionEventsValidatorTests
{
    private Mock<IQueryRepository<AbstractSubmissionEvent>> _eventsRepository;

    [TestInitialize]
    public void TestInitialize()
    {
        _eventsRepository = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>().BuildMock);
    }

    [TestMethod]
    public async Task IsSubmissionValidAsync_WithoutAnyEvents_ReturnsFalse()
    {
        var validator = CreateSubmissionEventsValidator();
        bool result = await validator.IsSubmissionValidAsync(Guid.Empty, Guid.Empty, CancellationToken.None);

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsSubmissionValidAsync_WithFailedAntivirusResult_ReturnsFalse()
    {
        // Arrange
        Guid submissionId = Guid.Parse("CDE4BC91-E1F3-4209-8EF2-17DF224F2203");
        Guid fileId = Guid.Parse("42F659CB-F797-4701-BDAF-E04CC0CB161C");

        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusResultEvent { FileId = fileId, AntivirusScanResult = AntivirusScanResult.Quarantined }
        };

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var validator = CreateSubmissionEventsValidator();
        bool result = await validator.IsSubmissionValidAsync(submissionId, fileId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsSubmissionValidAsync_WhenAntivirusResultHasNoMatchingValidationEvent_ReturnsFalse()
    {
        // Arrange
        Guid submissionId = Guid.Parse("CDE4BC91-E1F3-4209-8EF2-17DF224F2203");
        Guid fileId = Guid.Parse("42F659CB-F797-4701-BDAF-E04CC0CB161C");

        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusResultEvent { FileId = fileId, BlobName = "new-file", AntivirusScanResult = AntivirusScanResult.Success },
            new RegistrationValidationEvent(),
        };

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var validator = CreateSubmissionEventsValidator();
        bool result = await validator.IsSubmissionValidAsync(submissionId, fileId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsSubmissionValidAsync_WithFailedValidationEvent_ReturnsFalse()
    {
        // Arrange
        Guid submissionId = Guid.Parse("CDE4BC91-E1F3-4209-8EF2-17DF224F2203");
        Guid fileId = Guid.Parse("42F659CB-F797-4701-BDAF-E04CC0CB161C");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.CompanyDetails, fileId: fileId, isValid: false));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var validator = CreateSubmissionEventsValidator();
        bool result = await validator.IsSubmissionValidAsync(submissionId, fileId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsSubmissionValidAsync_WithSuccessfulValidationEvent_AndNoBrandsOrPartnersRequired_ReturnsTrue()
    {
        // Arrange
        Guid submissionId = Guid.Parse("CDE4BC91-E1F3-4209-8EF2-17DF224F2203");
        Guid fileId = Guid.Parse("42F659CB-F797-4701-BDAF-E04CC0CB161C");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.CompanyDetails, fileId: fileId));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var validator = CreateSubmissionEventsValidator();
        bool result = await validator.IsSubmissionValidAsync(submissionId, fileId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsSubmissionValidAsync_WhenRequiresBrands_AndBrandsFileNotValid_ReturnsFalse()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");
        Guid submissionId = Guid.Parse("CDE4BC91-E1F3-4209-8EF2-17DF224F2203");
        Guid orgFileId = Guid.Parse("42F659CB-F797-4701-BDAF-E04CC0CB161C");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.CompanyDetails, registrationSetId, fileId: orgFileId, requiresBrands: true));
        events.AddRange(CreateEventSet(FileType.Brands, registrationSetId, isValid: false));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var validator = CreateSubmissionEventsValidator();
        bool result = await validator.IsSubmissionValidAsync(submissionId, orgFileId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsSubmissionValidAsync_WhenRequiresBrands_WithValidBrandsValidationEvent_ReturnsTrue()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");
        Guid submissionId = Guid.Parse("CDE4BC91-E1F3-4209-8EF2-17DF224F2203");
        Guid orgFileId = Guid.Parse("42F659CB-F797-4701-BDAF-E04CC0CB161C");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.CompanyDetails, registrationSetId, fileId: orgFileId, requiresBrands: true));
        events.AddRange(CreateEventSet(FileType.Brands, registrationSetId, isValid: true));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var validator = CreateSubmissionEventsValidator();
        bool result = await validator.IsSubmissionValidAsync(submissionId, orgFileId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsSubmissionValidAsync_WhenRequiresPartners_AndBrandsFileNotValid_ReturnsFalse()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");
        Guid submissionId = Guid.Parse("CDE4BC91-E1F3-4209-8EF2-17DF224F2203");
        Guid orgFileId = Guid.Parse("42F659CB-F797-4701-BDAF-E04CC0CB161C");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.CompanyDetails, registrationSetId, requiresPartners: true, fileId: orgFileId));
        events.AddRange(CreateEventSet(FileType.Partnerships, registrationSetId, isValid: false));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var validator = CreateSubmissionEventsValidator();
        bool result = await validator.IsSubmissionValidAsync(submissionId, orgFileId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsSubmissionValidAsync_WhenRequiresPartners_WithValidPartnersValidationEvent_ReturnsTrue()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");
        Guid submissionId = Guid.Parse("CDE4BC91-E1F3-4209-8EF2-17DF224F2203");
        Guid orgFileId = Guid.Parse("42F659CB-F797-4701-BDAF-E04CC0CB161C");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.CompanyDetails, registrationSetId, requiresPartners: true, fileId: orgFileId));
        events.AddRange(CreateEventSet(FileType.Partnerships, registrationSetId, isValid: true));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var validator = CreateSubmissionEventsValidator();
        bool result = await validator.IsSubmissionValidAsync(submissionId, orgFileId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsSubmissionValidAsync_WhenRequiresBrandsAndPartners_WithValidEvents_ReturnsTrue()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");
        Guid submissionId = Guid.Parse("CDE4BC91-E1F3-4209-8EF2-17DF224F2203");
        Guid orgFileId = Guid.Parse("42F659CB-F797-4701-BDAF-E04CC0CB161C");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.CompanyDetails, registrationSetId, requiresBrands: true, requiresPartners: true, fileId: orgFileId));
        events.AddRange(CreateEventSet(FileType.Brands, registrationSetId, isValid: true));
        events.AddRange(CreateEventSet(FileType.Partnerships, registrationSetId, isValid: true));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var validator = CreateSubmissionEventsValidator();
        bool result = await validator.IsSubmissionValidAsync(submissionId, orgFileId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsPartnersFileValid_WithoutAntivirusCheck_ReturnsFalse()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");

        var events = new List<AbstractSubmissionEvent>();

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsPartnersFileValid(events, registrationSetId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsPartnersFileValid_WithDifferentRegistrationSetId_ReturnsFalse()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");
        Guid differentId = Guid.Parse("A2A27D73-0A1B-48A8-97CA-7CE3C2D9545D");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.Partnerships, registrationSetId));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsPartnersFileValid(events, differentId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsPartnersFileValid_WithFailedAntivirusResult_ReturnsFalse()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.Partnerships, registrationSetId, failedVirusScan: true));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsPartnersFileValid(events, registrationSetId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsPartnersFileValid_WhenRequiresRowValidationFalse_ReturnsTrue()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.Partnerships, registrationSetId, requiresRowValidation: false));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsPartnersFileValid(events, registrationSetId);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsPartnersFileValid_WithFailedValidationEvent_ReturnsFalse()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.Partnerships, registrationSetId, isValid: false));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsPartnersFileValid(events, registrationSetId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsPartnersFileValid_WithoutValidationEvent_ReturnsFalse()
    {
        // Arrange
        Guid fileId = Guid.Parse("015CCE0A-6969-4A92-BC7A-42DE6B328FC3");
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");

        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent { FileType = FileType.Partnerships, RegistrationSetId = registrationSetId, FileId = fileId },
            new AntivirusResultEvent { AntivirusScanResult = AntivirusScanResult.Success, FileId = fileId, RequiresRowValidation = true },
        };

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsPartnersFileValid(events, registrationSetId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsPartnersFileValid_WithoutRegistrationSetId_AndMultipleEvents_AndLatestEventIsValid_ReturnsTrue()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 19, 9, 0, 0, DateTimeKind.Utc);
        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.Partnerships, isValid: false, eventTimestamp: timestamp));
        events.AddRange(CreateEventSet(FileType.Partnerships, isValid: false, eventTimestamp: timestamp.AddDays(1)));
        events.AddRange(CreateEventSet(FileType.Partnerships, isValid: true, eventTimestamp: timestamp.AddDays(2)));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsPartnersFileValid(events, registrationSetId: null);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsBrandsFileValid_WithoutAntivirusCheck_ReturnsFalse()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");

        var events = new List<AbstractSubmissionEvent>();

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsBrandsFileValid(events, registrationSetId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsBrandsFileValid_WithDifferentRegistrationSetId_ReturnsFalse()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");
        Guid differentId = Guid.Parse("A2A27D73-0A1B-48A8-97CA-7CE3C2D9545D");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.Brands, registrationSetId));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsBrandsFileValid(events, differentId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsBrandsFileValid_WithFailedAntivirusResult_ReturnsFalse()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.Brands, registrationSetId, failedVirusScan: true));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsBrandsFileValid(events, registrationSetId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsBrandsFileValid_WhenRequiresRowValidationFalse_ReturnsTrue()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.Brands, registrationSetId, isValid: false, requiresRowValidation: false));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsBrandsFileValid(events, registrationSetId);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task IsBrandsFileValid_WithFailedValidationEvent_ReturnsFalse()
    {
        // Arrange
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.Brands, registrationSetId, isValid: false));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsBrandsFileValid(events, registrationSetId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsBrandsFileValid_WithoutValidationEvent_ReturnsFalse()
    {
        // Arrange
        Guid fileId = Guid.Parse("015CCE0A-6969-4A92-BC7A-42DE6B328FC3");
        Guid registrationSetId = Guid.Parse("F7C2A1BB-F6B9-4FF3-81A2-7791929ED35F");

        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent { FileType = FileType.Brands, RegistrationSetId = registrationSetId, FileId = fileId },
            new AntivirusResultEvent { AntivirusScanResult = AntivirusScanResult.Success, FileId = fileId, RequiresRowValidation = true },
        };

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsBrandsFileValid(events, registrationSetId);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task IsBrandsFileValid_WithoutRegistrationSetId_AndMultipleEvents_AndLatestEventIsValid_ReturnsTrue()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 19, 9, 0, 0, DateTimeKind.Utc);
        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(FileType.Brands, isValid: false, eventTimestamp: timestamp));
        events.AddRange(CreateEventSet(FileType.Brands, isValid: false, eventTimestamp: timestamp.AddDays(1)));
        events.AddRange(CreateEventSet(FileType.Brands, isValid: true, eventTimestamp: timestamp.AddDays(2)));

        _eventsRepository
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        bool result = SubmissionEventsValidator.IsBrandsFileValid(events, registrationSetId: null);

        // Assert
        result.Should().BeTrue();
    }

    private static IEnumerable<AbstractSubmissionEvent> CreateEventSet(
        FileType fileType,
        Guid? registrationSetId = null,
        Guid? fileId = null,
        bool failedVirusScan = false,
        bool requiresRowValidation = true,
        bool isValid = true,
        bool requiresBrands = false,
        bool requiresPartners = false,
        DateTime? eventTimestamp = null)
    {
        string blobName = Path.GetRandomFileName();
        var defaultFileId = Guid.NewGuid();

        yield return new AntivirusCheckEvent
        {
            Created = eventTimestamp.GetValueOrDefault(),
            FileId = fileId ?? defaultFileId,
            BlobName = blobName,
            FileType = fileType,
            RegistrationSetId = registrationSetId,
        };
        yield return new AntivirusResultEvent
        {
            Created = eventTimestamp.GetValueOrDefault().AddMinutes(1),
            FileId = fileId ?? defaultFileId,
            BlobName = blobName,
            AntivirusScanResult = failedVirusScan ? AntivirusScanResult.Quarantined : AntivirusScanResult.Success,
            RequiresRowValidation = requiresRowValidation,
        };

        if (failedVirusScan)
        {
            yield break;
        }

        if (fileType == FileType.CompanyDetails)
        {
            yield return new RegistrationValidationEvent
            {
                Created = eventTimestamp.GetValueOrDefault().AddMinutes(2),
                BlobName = blobName,
                IsValid = isValid,
                RequiresBrandsFile = requiresBrands,
                RequiresPartnershipsFile = requiresPartners,
            };
        }

        if (fileType == FileType.Brands && requiresRowValidation)
        {
            yield return new BrandValidationEvent
            {
                Created = eventTimestamp.GetValueOrDefault().AddMinutes(3),
                BlobName = blobName,
                IsValid = isValid,
            };
        }

        if (fileType == FileType.Partnerships && requiresRowValidation)
        {
            yield return new PartnerValidationEvent
            {
                BlobName = blobName,
                IsValid = isValid,
            };
        }
    }

    private SubmissionEventsValidator CreateSubmissionEventsValidator()
    {
        return new SubmissionEventsValidator(_eventsRepository.Object);
    }
}