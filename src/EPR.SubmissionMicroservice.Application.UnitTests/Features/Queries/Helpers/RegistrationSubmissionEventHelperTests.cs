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
public class RegistrationSubmissionEventHelperTests
{
    private Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventRepositoryMock;
    private RegistrationSubmissionGetResponse? _registrationSubmissionGetResponse;
    private RegistrationSubmissionEventHelper _systemUnderTest;

    [TestInitialize]
    public async Task TestInitialize()
    {
        _registrationSubmissionGetResponse = new RegistrationSubmissionGetResponse();
        _submissionEventRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _systemUnderTest = new RegistrationSubmissionEventHelper(_submissionEventRepositoryMock.Object);
    }

    [TestMethod]
    public async Task SetValidationEvents_DoesNotSetAnyProperties_WhenNoEventsExist()
    {
        // Arrange
        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>().BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = null,
            CompanyDetailsDataComplete = false,
            CompanyDetailsUploadedBy = null,
            CompanyDetailsUploadedDate = null,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            LastSubmittedFiles = null,
            ValidationPass = false,
        });
    }

    [TestMethod]
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenLatestEventIsAntivirusCheckEvent(Guid? registrationSetId)
    {
        // Arrange
        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = Guid.NewGuid(),
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = Guid.NewGuid(),
            RegistrationSetId = registrationSetId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent> { antivirusCheckEvent }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = antivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = false,
            CompanyDetailsUploadedBy = antivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = antivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            LastSubmittedFiles = null,
            ValidationPass = false,
        });
    }

    [TestMethod]
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenLatestEventIsAntivirusResultEvent(Guid? registrationSetId)
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = fileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var antivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            BlobName = "blob-name",
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent> { antivirusCheckEvent, antivirusResultEvent }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = antivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = false,
            CompanyDetailsUploadedBy = antivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = antivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            LastSubmittedFiles = null,
            ValidationPass = false,
        });
    }

    [TestMethod]
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenBrandsAndPartnershipsAreRequiredButHaveNotBeenUploadedYet(Guid? registrationSetId)
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string blobName = "blob-name";
        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = fileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var antivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            BlobName = blobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var registrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = blobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent> { antivirusCheckEvent, antivirusResultEvent, registrationValidationEvent }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            CompanyDetailsFileName = antivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = antivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = antivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            LastSubmittedFiles = null,
            ValidationPass = false,
        });
    }

    [TestMethod]
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenBrandsAndPartnershipsAreRequiredAndHaveBeenUploaded(Guid? registrationSetId)
    {
        // Arrange
        var companyDetailsFileId = Guid.NewGuid();
        var brandsFileId = Guid.NewGuid();
        var partnershipsFileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string companyDetailsBlobName = "company-details-blob-name";
        const string brandsBlobName = "brands-blob-name";
        const string partnershipsBlobName = "brands-blob-name";
        var companyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = companyDetailsFileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var companyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = companyDetailsFileId,
            BlobName = companyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var registrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = companyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var brandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = brandsFileId,
            FileName = "brands.csv",
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var brandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = brandsFileId,
            BlobName = brandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId
        };
        var partnershipsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = partnershipsFileId,
            FileName = "partnerships.csv",
            FileType = FileType.Partnerships,
            Created = DateTime.Now.AddMinutes(7),
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var partnershipsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = partnershipsFileId,
            BlobName = partnershipsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(8),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                companyDetailsAntivirusCheckEvent,
                companyDetailsAntivirusResultEvent,
                registrationValidationEvent,
                brandsAntivirusCheckEvent,
                brandsAntivirusResultEvent,
                partnershipsAntivirusCheckEvent,
                partnershipsAntivirusResultEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            CompanyDetailsFileName = companyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = companyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = companyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = brandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsUploadedBy = brandsAntivirusCheckEvent.UserId,
            BrandsUploadedDate = brandsAntivirusCheckEvent.Created,
            PartnershipsFileName = partnershipsAntivirusCheckEvent.FileName,
            PartnershipsDataComplete = true,
            PartnershipsUploadedBy = partnershipsAntivirusCheckEvent.UserId,
            PartnershipsUploadedDate = partnershipsAntivirusCheckEvent.Created,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = companyDetailsAntivirusCheckEvent.FileId,
                CompanyDetailsFileName = companyDetailsAntivirusCheckEvent.FileName,
                CompanyDetailsUploadedBy = companyDetailsAntivirusCheckEvent.UserId,
                CompanyDetailsUploadDatetime = companyDetailsAntivirusCheckEvent.Created,
                BrandsFileName = brandsAntivirusCheckEvent.FileName,
                BrandsUploadedBy = brandsAntivirusCheckEvent.UserId,
                BrandsUploadDatetime = brandsAntivirusCheckEvent.Created,
                PartnershipsFileName = partnershipsAntivirusCheckEvent.FileName,
                PartnershipsUploadedBy = partnershipsAntivirusCheckEvent.UserId,
                PartnershipsUploadDatetime = partnershipsAntivirusCheckEvent.Created
            },
            LastSubmittedFiles = null,
            ValidationPass = true,
        });
    }

    [TestMethod]
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenBrandsAndPartnershipsAreRequiredButOnlyBrandsHasBeenUploaded(Guid? registrationSetId)
    {
        // Arrange
        var companyDetailsFileId = Guid.NewGuid();
        var brandsFileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string companyDetailsBlobName = "company-details-blob-name";
        const string brandsBlobName = "brands-blob-name";
        var companyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = companyDetailsFileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var companyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = companyDetailsFileId,
            BlobName = companyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var registrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = companyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var brandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = brandsFileId,
            FileName = "brands.csv",
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var brandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = brandsFileId,
            BlobName = brandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                companyDetailsAntivirusCheckEvent,
                companyDetailsAntivirusResultEvent,
                registrationValidationEvent,
                brandsAntivirusCheckEvent,
                brandsAntivirusResultEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            CompanyDetailsFileName = companyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = companyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = companyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = brandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsUploadedBy = brandsAntivirusCheckEvent.UserId,
            BrandsUploadedDate = brandsAntivirusCheckEvent.Created,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            LastSubmittedFiles = null,
            ValidationPass = false,
        });
    }

    [TestMethod]
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenBrandsIsRequiredButHasNotBeenUploadedYet(Guid? registrationSetId)
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string blobName = "blob-name";
        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = fileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var antivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            BlobName = blobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var registrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = blobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent> { antivirusCheckEvent, antivirusResultEvent, registrationValidationEvent }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = antivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = antivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = antivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            LastSubmittedFiles = null,
            ValidationPass = false,
        });
    }

    [TestMethod]
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenBrandsIsRequiredAndHasBeenUploaded(Guid? registrationSetId)
    {
        // Arrange
        var companyDetailsFileId = Guid.NewGuid();
        var brandsFileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string companyDetailsBlobName = "company-details-blob-name";
        const string brandsBlobName = "brands-blob-name";
        var companyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = companyDetailsFileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var companyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = companyDetailsFileId,
            BlobName = companyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var registrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = companyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var brandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = brandsFileId,
            FileName = "brands.csv",
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var brandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = brandsFileId,
            BlobName = brandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                companyDetailsAntivirusCheckEvent,
                companyDetailsAntivirusResultEvent,
                registrationValidationEvent,
                brandsAntivirusCheckEvent,
                brandsAntivirusResultEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = companyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = companyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = companyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = brandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsUploadedBy = brandsAntivirusCheckEvent.UserId,
            BrandsUploadedDate = brandsAntivirusCheckEvent.Created,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = companyDetailsAntivirusCheckEvent.FileId,
                CompanyDetailsFileName = companyDetailsAntivirusCheckEvent.FileName,
                CompanyDetailsUploadedBy = companyDetailsAntivirusCheckEvent.UserId,
                CompanyDetailsUploadDatetime = companyDetailsAntivirusCheckEvent.Created,
                BrandsFileName = brandsAntivirusCheckEvent.FileName,
                BrandsUploadedBy = brandsAntivirusCheckEvent.UserId,
                BrandsUploadDatetime = brandsAntivirusCheckEvent.Created,
                PartnershipsFileName = null,
                PartnershipsUploadedBy = null,
                PartnershipsUploadDatetime = null
            },
            LastSubmittedFiles = null,
            ValidationPass = true,
        });
    }

    [TestMethod]
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenPartnershipsIsRequiredButHasNotBeenUploadedYet(Guid? registrationSetId)
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string blobName = "blob-name";
        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = fileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var antivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            BlobName = blobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var registrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = blobName,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = true,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent> { antivirusCheckEvent, antivirusResultEvent, registrationValidationEvent }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = true,
            CompanyDetailsFileName = antivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = antivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = antivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            LastSubmittedFiles = null,
            ValidationPass = false,
        });
    }

    [TestMethod]
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenPartnershipsIsRequiredAndHasBeenUploaded(Guid? registrationSetId)
    {
        // Arrange
        var companyDetailsFileId = Guid.NewGuid();
        var partnershipsFileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string companyDetailsBlobName = "company-details-blob-name";
        const string partnershipsBlobName = "partnerships-blob-name";
        var companyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = companyDetailsFileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var companyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = companyDetailsFileId,
            BlobName = companyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var registrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = companyDetailsBlobName,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = true,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var partnershipsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = partnershipsFileId,
            FileName = "partnerships.csv",
            FileType = FileType.Partnerships,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var partnershipsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = partnershipsFileId,
            BlobName = partnershipsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                companyDetailsAntivirusCheckEvent,
                companyDetailsAntivirusResultEvent,
                registrationValidationEvent,
                partnershipsAntivirusCheckEvent,
                partnershipsAntivirusResultEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = true,
            CompanyDetailsFileName = companyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = companyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = companyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = partnershipsAntivirusCheckEvent.FileName,
            PartnershipsDataComplete = true,
            PartnershipsUploadedBy = partnershipsAntivirusCheckEvent.UserId,
            PartnershipsUploadedDate = partnershipsAntivirusCheckEvent.Created,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = companyDetailsAntivirusCheckEvent.FileId,
                CompanyDetailsFileName = companyDetailsAntivirusCheckEvent.FileName,
                CompanyDetailsUploadedBy = companyDetailsAntivirusCheckEvent.UserId,
                CompanyDetailsUploadDatetime = companyDetailsAntivirusCheckEvent.Created,
                BrandsFileName = null,
                BrandsUploadedBy = null,
                BrandsUploadDatetime = null,
                PartnershipsFileName = partnershipsAntivirusCheckEvent.FileName,
                PartnershipsUploadedBy = partnershipsAntivirusCheckEvent.UserId,
                PartnershipsUploadDatetime = partnershipsAntivirusCheckEvent.Created
            },
            ValidationPass = true,
        });
    }

    [TestMethod]
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenBrandsAndPartnershipsAreNotRequired(Guid? registrationSetId)
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string blobName = "blob-name";
        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = fileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = registrationSetId
        };
        var antivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            BlobName = blobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var registrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = blobName,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent> { antivirusCheckEvent, antivirusResultEvent, registrationValidationEvent }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = antivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = antivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = antivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = antivirusCheckEvent.FileId,
                CompanyDetailsFileName = antivirusCheckEvent.FileName,
                CompanyDetailsUploadedBy = antivirusCheckEvent.UserId,
                CompanyDetailsUploadDatetime = antivirusCheckEvent.Created,
                BrandsFileName = null,
                BrandsUploadedBy = null,
                BrandsUploadDatetime = null,
                PartnershipsFileName = null,
                PartnershipsUploadedBy = null,
                PartnershipsUploadDatetime = null
            },
            LastSubmittedFiles = null,
            ValidationPass = true,
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenLatestUploadedFilesAreNotValidButAPreviousUploadsAre()
    {
        // Arrange
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        const string uploadOneBrandsBlobName = "upload-one-brands-blob-name";
        const string uploadTwoCompanyDetailsBlobName = "upload-two-company-details-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneBrandsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneRegistationSetId = Guid.NewGuid();
        var uploadTwoRegistationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = uploadOneRegistationSetId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var uploadOneBrandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneBrandsFileId,
            FileName = "brands-one.csv",
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(3),
            UserId = userId,
            RegistrationSetId = uploadOneRegistationSetId
        };
        var uploadOneBrandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneBrandsFileId,
            BlobName = uploadOneBrandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(4),
            UserId = userId
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistationSetId
        };
        var uploadTwoCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            BlobName = uploadTwoCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId
        };
        var uploadTwoRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadTwoCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(7),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneCompanyDetailsAntivirusResultEvent,
                uploadOneRegistrationValidationEvent,
                uploadOneBrandsAntivirusCheckEvent,
                uploadOneBrandsAntivirusResultEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent,
                uploadTwoCompanyDetailsAntivirusResultEvent,
                uploadTwoRegistrationValidationEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = uploadOneCompanyDetailsAntivirusCheckEvent.FileId,
                CompanyDetailsFileName = uploadOneCompanyDetailsAntivirusCheckEvent.FileName,
                CompanyDetailsUploadedBy = uploadOneCompanyDetailsAntivirusCheckEvent.UserId,
                CompanyDetailsUploadDatetime = uploadOneCompanyDetailsAntivirusCheckEvent.Created,
                BrandsFileName = uploadOneBrandsAntivirusCheckEvent.FileName,
                BrandsUploadedBy = uploadOneBrandsAntivirusCheckEvent.UserId,
                BrandsUploadDatetime = uploadOneBrandsAntivirusCheckEvent.Created,
                PartnershipsFileName = null,
                PartnershipsUploadedBy = null,
                PartnershipsUploadDatetime = null
            },
            LastSubmittedFiles = null,
            ValidationPass = false,
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenSubmittedSubmissionHasAllRequiredFilesUploadedAfterSubmission()
    {
        // Arrange
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        const string uploadTwoCompanyDetailsBlobName = "upload-two-company-details-blob-name";
        const string uploadTwoBrandsBlobName = "upload-two-brands-blob-name";
        const string uploadTwoPartnershipsBlobName = "upload-two-partnerships-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadTwoBrandsFileId = Guid.NewGuid();
        var uploadTwoPartnershipsFileId = Guid.NewGuid();
        var uploadOneRegistrationSetId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var uploadOneSubmittedEvent = new SubmittedEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            Created = DateTime.Now.AddMinutes(3),
            UserId = userId
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(4),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };
        var uploadTwoCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            BlobName = uploadTwoCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId
        };
        var uploadTwoRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadTwoCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId
        };
        var uploadTwoBrandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoBrandsFileId,
            FileName = "brands-two.csv",
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(7),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };
        var uploadTwoBrandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoBrandsFileId,
            BlobName = uploadTwoBrandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(8),
            UserId = userId
        };
        var uploadTwoPartnershipsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoPartnershipsFileId,
            FileName = "partnerships-two.csv",
            FileType = FileType.Partnerships,
            Created = DateTime.Now.AddMinutes(9),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };
        var uploadTwoPartnershipsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoPartnershipsFileId,
            BlobName = uploadTwoPartnershipsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(10),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneCompanyDetailsAntivirusResultEvent,
                uploadOneRegistrationValidationEvent,
                uploadOneSubmittedEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent,
                uploadTwoCompanyDetailsAntivirusResultEvent,
                uploadTwoRegistrationValidationEvent,
                uploadTwoBrandsAntivirusCheckEvent,
                uploadTwoBrandsAntivirusResultEvent,
                uploadTwoPartnershipsAntivirusCheckEvent,
                uploadTwoPartnershipsAntivirusResultEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = uploadTwoBrandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsUploadedBy = uploadTwoBrandsAntivirusCheckEvent.UserId,
            BrandsUploadedDate = uploadTwoBrandsAntivirusCheckEvent.Created,
            PartnershipsFileName = uploadTwoPartnershipsAntivirusCheckEvent.FileName,
            PartnershipsDataComplete = true,
            PartnershipsUploadedBy = uploadTwoPartnershipsAntivirusCheckEvent.UserId,
            PartnershipsUploadedDate = uploadTwoPartnershipsAntivirusCheckEvent.Created,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = uploadTwoCompanyDetailsAntivirusCheckEvent.FileId,
                CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
                CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
                CompanyDetailsUploadDatetime = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
                BrandsFileName = uploadTwoBrandsAntivirusCheckEvent.FileName,
                BrandsUploadedBy = uploadTwoBrandsAntivirusCheckEvent.UserId,
                BrandsUploadDatetime = uploadTwoBrandsAntivirusCheckEvent.Created,
                PartnershipsFileName = uploadTwoPartnershipsAntivirusCheckEvent.FileName,
                PartnershipsUploadedBy = uploadTwoPartnershipsAntivirusCheckEvent.UserId,
                PartnershipsUploadDatetime = uploadTwoPartnershipsAntivirusCheckEvent.Created
            },
            LastSubmittedFiles = new SubmittedRegistrationFilesInformation
            {
                CompanyDetailsFileName = uploadOneCompanyDetailsAntivirusCheckEvent.FileName,
                BrandsFileName = null,
                PartnersFileName = null,
                SubmittedBy = uploadOneSubmittedEvent.UserId,
                SubmittedDateTime = uploadOneSubmittedEvent.Created
            },
            ValidationPass = true,
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenSubmittedSubmissionHasNotAllRequiredFilesUploadedAfterSubmission()
    {
        // Arrange
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        const string uploadTwoCompanyDetailsBlobName = "upload-two-company-details-blob-name";
        const string uploadTwoBrandsBlobName = "upload-two-brands-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadTwoBrandsFileId = Guid.NewGuid();
        var uploadOneRegistrationSetId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var uploadOneSubmittedEvent = new SubmittedEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            Created = DateTime.Now.AddMinutes(3),
            UserId = userId
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(4),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };
        var uploadTwoCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            BlobName = uploadTwoCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId
        };
        var uploadTwoRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadTwoCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId
        };
        var uploadTwoBrandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoBrandsFileId,
            FileName = "brands-two.csv",
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(7),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };
        var uploadTwoBrandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoBrandsFileId,
            BlobName = uploadTwoBrandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(8),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneCompanyDetailsAntivirusResultEvent,
                uploadOneRegistrationValidationEvent,
                uploadOneSubmittedEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent,
                uploadTwoCompanyDetailsAntivirusResultEvent,
                uploadTwoRegistrationValidationEvent,
                uploadTwoBrandsAntivirusCheckEvent,
                uploadTwoBrandsAntivirusResultEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = uploadTwoBrandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsUploadedBy = uploadTwoBrandsAntivirusCheckEvent.UserId,
            BrandsUploadedDate = uploadTwoBrandsAntivirusCheckEvent.Created,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = uploadOneCompanyDetailsAntivirusCheckEvent.FileId,
                CompanyDetailsFileName = uploadOneCompanyDetailsAntivirusCheckEvent.FileName,
                CompanyDetailsUploadedBy = uploadOneCompanyDetailsAntivirusCheckEvent.UserId,
                CompanyDetailsUploadDatetime = uploadOneCompanyDetailsAntivirusCheckEvent.Created,
                BrandsFileName = null,
                BrandsUploadedBy = null,
                BrandsUploadDatetime = null,
                PartnershipsFileName = null,
                PartnershipsUploadedBy = null,
                PartnershipsUploadDatetime = null
            },
            LastSubmittedFiles = new SubmittedRegistrationFilesInformation
            {
                CompanyDetailsFileName = uploadOneCompanyDetailsAntivirusCheckEvent.FileName,
                BrandsFileName = null,
                PartnersFileName = null,
                SubmittedBy = uploadOneSubmittedEvent.UserId,
                SubmittedDateTime = uploadOneSubmittedEvent.Created
            },
            ValidationPass = false,
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenSubmittedSubmissionWithoutRegistrationSetIdHasAllRequiredFilesUploadedAfterSubmission()
    {
        // Arrange
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        const string uploadTwoCompanyDetailsBlobName = "upload-two-company-details-blob-name";
        const string uploadOneBrandsBlobName = "upload-one-brands-blob-name";
        const string uploadTwoBrandsBlobName = "upload-two-brands-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneBrandsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadTwoBrandsFileId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var uploadOneBrandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneBrandsFileId,
            FileName = "brands-one.csv",
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(3),
            UserId = userId
        };
        var uploadOneBrandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneBrandsFileId,
            BlobName = uploadOneBrandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(4),
            UserId = userId
        };
        var uploadOneSubmittedEvent = new SubmittedEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };
        var uploadTwoCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            BlobName = uploadTwoCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(7),
            UserId = userId
        };
        var uploadTwoRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadTwoCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(8),
            UserId = userId
        };
        var uploadTwoBrandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoBrandsFileId,
            FileName = "brands-two.csv",
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(9),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };
        var uploadTwoBrandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoBrandsFileId,
            BlobName = uploadTwoBrandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(10),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneCompanyDetailsAntivirusResultEvent,
                uploadOneRegistrationValidationEvent,
                uploadOneBrandsAntivirusCheckEvent,
                uploadOneBrandsAntivirusResultEvent,
                uploadOneSubmittedEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent,
                uploadTwoCompanyDetailsAntivirusResultEvent,
                uploadTwoRegistrationValidationEvent,
                uploadTwoBrandsAntivirusCheckEvent,
                uploadTwoBrandsAntivirusResultEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = uploadTwoBrandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsUploadedBy = uploadTwoBrandsAntivirusCheckEvent.UserId,
            BrandsUploadedDate = uploadTwoBrandsAntivirusCheckEvent.Created,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = uploadTwoCompanyDetailsAntivirusCheckEvent.FileId,
                CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
                CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
                CompanyDetailsUploadDatetime = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
                BrandsFileName = uploadTwoBrandsAntivirusCheckEvent.FileName,
                BrandsUploadedBy = uploadTwoBrandsAntivirusCheckEvent.UserId,
                BrandsUploadDatetime = uploadTwoBrandsAntivirusCheckEvent.Created,
                PartnershipsFileName = null,
                PartnershipsUploadedBy = null,
                PartnershipsUploadDatetime = null
            },
            LastSubmittedFiles = new SubmittedRegistrationFilesInformation
            {
                CompanyDetailsFileName = uploadOneCompanyDetailsAntivirusCheckEvent.FileName,
                BrandsFileName = uploadOneBrandsAntivirusCheckEvent.FileName,
                PartnersFileName = null,
                SubmittedBy = uploadOneSubmittedEvent.UserId,
                SubmittedDateTime = uploadOneSubmittedEvent.Created
            },
            ValidationPass = true,
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenNotSubmittedButTwoSetsOfFilesHaveBeenUploadedWithTheFirstHavingNoRegistrationSetId()
    {
        // Arrange
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        const string uploadTwoCompanyDetailsBlobName = "upload-two-company-details-blob-name";
        const string uploadOneBrandsBlobName = "upload-one-brands-blob-name";
        const string uploadTwoBrandsBlobName = "upload-two-brands-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneBrandsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadTwoBrandsFileId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var uploadOneBrandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneBrandsFileId,
            FileName = "brands-one.csv",
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(3),
            UserId = userId
        };
        var uploadOneBrandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneBrandsFileId,
            BlobName = uploadOneBrandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(4),
            UserId = userId
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };
        var uploadTwoCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            BlobName = uploadTwoCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(7),
            UserId = userId
        };
        var uploadTwoRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadTwoCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(8),
            UserId = userId
        };
        var uploadTwoBrandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoBrandsFileId,
            FileName = "brands-two.csv",
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(9),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };
        var uploadTwoBrandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoBrandsFileId,
            BlobName = uploadTwoBrandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(10),
            UserId = userId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneCompanyDetailsAntivirusResultEvent,
                uploadOneRegistrationValidationEvent,
                uploadOneBrandsAntivirusCheckEvent,
                uploadOneBrandsAntivirusResultEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent,
                uploadTwoCompanyDetailsAntivirusResultEvent,
                uploadTwoRegistrationValidationEvent,
                uploadTwoBrandsAntivirusCheckEvent,
                uploadTwoBrandsAntivirusResultEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = uploadTwoBrandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsUploadedBy = uploadTwoBrandsAntivirusCheckEvent.UserId,
            BrandsUploadedDate = uploadTwoBrandsAntivirusCheckEvent.Created,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = uploadTwoCompanyDetailsAntivirusCheckEvent.FileId,
                CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
                CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
                CompanyDetailsUploadDatetime = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
                BrandsFileName = uploadTwoBrandsAntivirusCheckEvent.FileName,
                BrandsUploadedBy = uploadTwoBrandsAntivirusCheckEvent.UserId,
                BrandsUploadDatetime = uploadTwoBrandsAntivirusCheckEvent.Created,
                PartnershipsFileName = null,
                PartnershipsUploadedBy = null,
                PartnershipsUploadDatetime = null
            },
            ValidationPass = true,
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenAnErrorIsPresentInTheAntivirusResultEvent()
    {
        // Arrange
        var errors = new List<string> { "error" };
        const string blobName = "blob-name";
        var userId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var companyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = fileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId
        };
        var companyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            BlobName = blobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId,
            Errors = errors
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent> { companyDetailsAntivirusCheckEvent, companyDetailsAntivirusResultEvent }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = companyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = false,
            CompanyDetailsUploadedBy = companyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = companyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            ValidationPass = false,
            Errors = errors
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenAnErrorIsPresentInTheRegistrationValidationEvent()
    {
        // Arrange
        var errors = new List<string> { "error" };
        const string blobName = "blob-name";
        var userId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var companyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = fileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId
        };
        var companyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            BlobName = blobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var registrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = blobName,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId,
            Errors = errors
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                companyDetailsAntivirusCheckEvent,
                companyDetailsAntivirusResultEvent,
                registrationValidationEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = companyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = companyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = companyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            ValidationPass = false,
            Errors = errors
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenThereAreMultipleInvalidUploadsWithTheFirstMissingCompanyDetailsAntivirusResultEvent()
    {
        // Arrange
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneRegistrationSetId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId,
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneRegistrationValidationEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = false,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            ValidationPass = false
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenThereAreMultipleInvalidUploadsWithTheFirstFailingCompanyDetailsAntivirusScan()
    {
        // Arrange
        var errors = new List<string> { "90" };
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneRegistrationSetId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Quarantined,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId,
            Errors = errors
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId,
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneCompanyDetailsAntivirusResultEvent,
                uploadOneRegistrationValidationEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = false,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            ValidationPass = false
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenThereAreMultipleInvalidUploadsWithTheFirstRegistrationValidationEventContainingErrors()
    {
        // Arrange
        var errors = new List<string> { "90" };
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneRegistrationSetId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId,
            Errors = errors
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneCompanyDetailsAntivirusResultEvent,
                uploadOneRegistrationValidationEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = false,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            ValidationPass = false
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenThereAreMultipleInvalidUploadsWithTheFirstRegistrationValidationEventMissing()
    {
        // Arrange
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneRegistrationSetId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneCompanyDetailsAntivirusResultEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = false,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            ValidationPass = false
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenThereAreMultipleInvalidUploadsWithTheFirstBrandsAntivirusResultEventMissing()
    {
        // Arrange
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneBrandsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneRegistrationSetId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var uploadOneBrandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneBrandsFileId,
            FileName = "brands-one.csv",
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(3),
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneCompanyDetailsAntivirusResultEvent,
                uploadOneRegistrationValidationEvent,
                uploadOneBrandsAntivirusCheckEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = false,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            ValidationPass = false
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenThereAreMultipleInvalidUploadsWithTheFirstBrandsAntivirusResultEventFailingScan()
    {
        // Arrange
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneBrandsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneRegistrationSetId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var uploadOneBrandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneBrandsFileId,
            FileName = "brands-one.csv",
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(3),
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneBrandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneBrandsFileId,
            AntivirusScanResult = AntivirusScanResult.Quarantined,
            Created = DateTime.Now.AddMinutes(4),
            UserId = userId
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneCompanyDetailsAntivirusResultEvent,
                uploadOneRegistrationValidationEvent,
                uploadOneBrandsAntivirusCheckEvent,
                uploadOneBrandsAntivirusResultEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = false,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            ValidationPass = false
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenThereAreMultipleInvalidUploadsWithTheFirstPartnershipsAntivirusResultEventMissing()
    {
        // Arrange
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadOnePartnershipsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneRegistrationSetId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = true,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var uploadOnePartnershipsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOnePartnershipsFileId,
            FileName = "partnerships-one.csv",
            FileType = FileType.Partnerships,
            Created = DateTime.Now.AddMinutes(3),
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneCompanyDetailsAntivirusResultEvent,
                uploadOneRegistrationValidationEvent,
                uploadOnePartnershipsAntivirusCheckEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = false,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            ValidationPass = false
        });
    }

    [TestMethod]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenThereAreMultipleInvalidUploadsWithTheFirstPartnershipsAntivirusResultEventFailingScan()
    {
        // Arrange
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadOnePartnershipsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneRegistrationSetId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(1),
            UserId = userId
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId
        };
        var uploadOnePartnershipsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOnePartnershipsFileId,
            FileName = "partnerships-one.csv",
            FileType = FileType.Partnerships,
            Created = DateTime.Now.AddMinutes(3),
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOnePartnershipsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOnePartnershipsFileId,
            AntivirusScanResult = AntivirusScanResult.Quarantined,
            Created = DateTime.Now.AddMinutes(4),
            UserId = userId
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                uploadOneCompanyDetailsAntivirusCheckEvent,
                uploadOneCompanyDetailsAntivirusResultEvent,
                uploadOneRegistrationValidationEvent,
                uploadOnePartnershipsAntivirusCheckEvent,
                uploadOnePartnershipsAntivirusResultEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = false,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            LastUploadedValidFiles = null,
            ValidationPass = false
        });
    }

    [TestMethod]
    public async Task VerifyFileIdIsForValidFileAsync_ReturnsTrue_WhenAntivirusResultEventExists()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var antivirusResultEvent = new AntivirusResultEvent { SubmissionId = submissionId, FileId = fileId };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent> { antivirusResultEvent }.BuildMock);

        // Act
        var result = await _systemUnderTest.VerifyFileIdIsForValidFileAsync(submissionId, fileId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task VerifyFileIdIsForValidFileAsync_ReturnsFalse_WhenAntivirusResultEventDoesNotExist()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>().BuildMock);

        // Act
        var result = await _systemUnderTest.VerifyFileIdIsForValidFileAsync(Guid.NewGuid(), fileId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    private static IEnumerable<object[]> RegistrationSetIdTestParameters()
    {
        yield return new object[] { Guid.NewGuid() };
        yield return new object[] { null };
    }
}