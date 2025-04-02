using System.Globalization;

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
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenValidBrandsAndPartnershipsAreRequiredAndHaveBeenUploaded(Guid? registrationSetId)
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
            UserId = userId,
            IsValid = true,
            WarningCount = 1
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
        var brandValidationEvent = new BrandValidationEvent
        {
            BlobName = brandsBlobName,
            Created = DateTime.Now.AddMinutes(-6),
            UserId = userId,
            IsValid = true
        };
        var partnerValidationEvent = new PartnerValidationEvent
        {
            BlobName = partnershipsBlobName,
            Created = DateTime.Now.AddMinutes(-6),
            UserId = userId,
            IsValid = true
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
                partnershipsAntivirusResultEvent,
                brandValidationEvent,
                partnerValidationEvent,
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
            CompanyDetailsFileIsValid = true,
            HasWarnings = true,
            BrandsFileName = brandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsDataIsValid = true,
            BrandsUploadedBy = brandsAntivirusCheckEvent.UserId,
            BrandsUploadedDate = brandsAntivirusCheckEvent.Created,
            PartnershipsFileName = partnershipsAntivirusCheckEvent.FileName,
            PartnershipsDataComplete = true,
            PartnersDataIsValid = true,
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
            OrganisationMemberCount = registrationValidationEvent.OrganisationMemberCount
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
            BrandsDataIsValid = true,
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
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenValidBrandsIsRequiredAndHasBeenUploaded(Guid? registrationSetId)
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
            UserId = userId,
            IsValid = true,
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
        var brandValidationEvent = new BrandValidationEvent
        {
            BlobName = brandsBlobName,
            Created = DateTime.Now.AddMinutes(7),
            UserId = userId,
            IsValid = true
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
                brandValidationEvent,
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
            CompanyDetailsFileIsValid = true,
            BrandsFileName = brandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsDataIsValid = true,
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
    public async Task SetValidationEvents_WhenBrandsIsRequiredAndHasBeenUploaded_AndHasErrors_VerifyBrandDataNotValid(Guid? registrationSetId)
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
            UserId = userId,
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
            UserId = userId,
            RequiresRowValidation = true,
        };
        var brandValidationEvent = new BrandValidationEvent
        {
            BlobName = brandsBlobName,
            Errors = new List<string> { "801" },
            Created = DateTime.Now.AddMinutes(-6),
            UserId = userId,
            IsValid = false
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
                brandValidationEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.BrandsDataIsValid.Should().BeFalse();
        _registrationSubmissionGetResponse.Errors.Should().Contain("801");
    }

    [TestMethod]
    public async Task SetValidationEvents_WhenBrandsIsRequiredAndHasBeenUploaded_AndVirusCheckFailed_VerifyBrandDataNotValid()
    {
        // Arrange
        Guid? registrationSetId = Guid.NewGuid();
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
            UserId = userId,
            RequiresRowValidation = true,
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
            Errors = new List<string> { "81" },
            AntivirusScanResult = AntivirusScanResult.Quarantined,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId,
            RequiresRowValidation = true,
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
        _registrationSubmissionGetResponse.BrandsDataComplete.Should().BeFalse();
        _registrationSubmissionGetResponse.BrandsDataIsValid.Should().BeFalse();
        _registrationSubmissionGetResponse.Errors.Should().Contain("81");
    }

    [TestMethod]
    [DataRow(true, false, DisplayName = "BrandFileHasErrors_WhenRequiresRowValidationIsTrue_SetsNotValid")]
    [DataRow(false, true, DisplayName = "BrandFileHasErrors_WhenRequiresRowValidationIsFalse_SetsIsValid")]
    public async Task SetValidationEvents_WhenBrandsIsRequiredAndHasBeenUploaded_AndHasErrors_WithRequiresRowValidationSet_VerifyBrandDataIsValid(bool rowValidationEnabled, bool isBrandDataValid)
    {
        // Arrange
        var companyDetailsFileId = Guid.NewGuid();
        var brandsFileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string companyDetailsBlobName = "company-details-blob-name";
        const string brandsBlobName = "brands-blob-name";
        const string expectedErrorCode = "801";
        Guid? registrationSetId = Guid.NewGuid();
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
            UserId = userId,
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
            UserId = userId,
            RequiresRowValidation = rowValidationEnabled,
        };
        var brandValidationEvent = new BrandValidationEvent
        {
            BlobName = brandsBlobName,
            Errors = new List<string> { expectedErrorCode },
            Created = DateTime.Now.AddMinutes(-6),
            UserId = userId,
            IsValid = false
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
                brandValidationEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.BrandsDataIsValid.Should().Be(isBrandDataValid);
        _registrationSubmissionGetResponse.BrandsDataComplete.Should().Be(true);
        if (rowValidationEnabled)
        {
            _registrationSubmissionGetResponse.Errors.Should().Contain(expectedErrorCode);
        }
        else
        {
            _registrationSubmissionGetResponse.Errors.Should().BeEmpty();
        }
    }

    [TestMethod]
    [DataRow(true, false, DisplayName = "PartnerFileHasErrors_WhenRequiresRowValidationIsTrue_SetsNotValid")]
    [DataRow(false, true, DisplayName = "PartnerFileHasErrors_WhenRequiresRowValidationIsFalse_SetsIsValid")]
    public async Task SetValidationEvents_WhenPartnershipsIsRequiredAndHasBeenUploaded_AndHasErrors_VerifyPartnersDataNotValid(bool rowValidationEnabled, bool isPartnerDataValid)
    {
        // Arrange
        Guid? registrationSetId = Guid.NewGuid();
        var companyDetailsFileId = Guid.NewGuid();
        var partnershipsFileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string expectedErrorCode = "801";
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
            UserId = userId,
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
            UserId = userId,
            RequiresRowValidation = rowValidationEnabled,
        };
        var partnerValidationEvent = new PartnerValidationEvent
        {
            BlobName = partnershipsBlobName,
            Errors = new List<string> { expectedErrorCode },
            Created = DateTime.Now.AddMinutes(-6),
            UserId = userId,
            IsValid = false
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                companyDetailsAntivirusCheckEvent,
                companyDetailsAntivirusResultEvent,
                registrationValidationEvent,
                partnershipsAntivirusCheckEvent,
                partnershipsAntivirusResultEvent,
                partnerValidationEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.PartnersDataIsValid.Should().Be(isPartnerDataValid);
        _registrationSubmissionGetResponse.PartnershipsDataComplete.Should().Be(true);
        if (rowValidationEnabled)
        {
            _registrationSubmissionGetResponse.Errors.Should().Contain(expectedErrorCode);
        }
        else
        {
            _registrationSubmissionGetResponse.Errors.Should().BeEmpty();
        }
    }

    [TestMethod]
    public async Task SetValidationEvents_WhenPartnershipsIsRequiredAndHasBeenUploaded_AndVirusCheckFailed_VerifyPartnersDataNotValid()
    {
        // Arrange
        Guid? registrationSetId = Guid.NewGuid();
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
            UserId = userId,
            RequiresRowValidation = true,
        };
        var registrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = companyDetailsBlobName,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = true,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId,
            IsValid = true,
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
            Errors = new List<string> { "81" },
            AntivirusScanResult = AntivirusScanResult.Quarantined,
            Created = DateTime.Now.AddMinutes(6),
            UserId = userId,
            RequiresRowValidation = true,
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
        _registrationSubmissionGetResponse.PartnershipsDataComplete.Should().BeFalse();
        _registrationSubmissionGetResponse.PartnersDataIsValid.Should().BeFalse();
        _registrationSubmissionGetResponse.Errors.Should().Contain("81");
    }

    [TestMethod]
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_WhenPartnershipsIsRequiredAndHasBeenUploaded_AndRequiresRowValidationIsFalse_VerifyPartnersDataIsValid(Guid? registrationSetId)
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
            UserId = userId,
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
            UserId = userId,
            RequiresRowValidation = false,
        };
        var partnerValidationEvent = new PartnerValidationEvent
        {
            BlobName = partnershipsBlobName,
            Errors = new List<string> { "801" },
            Created = DateTime.Now.AddMinutes(-6),
            UserId = userId,
            IsValid = false
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                companyDetailsAntivirusCheckEvent,
                companyDetailsAntivirusResultEvent,
                registrationValidationEvent,
                partnershipsAntivirusCheckEvent,
                partnershipsAntivirusResultEvent,
                partnerValidationEvent
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.PartnersDataIsValid.Should().BeTrue();
        _registrationSubmissionGetResponse.Errors.Should().BeEmpty();
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
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenValidPartnershipsIsRequiredAndHasBeenUploaded(Guid? registrationSetId)
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
            UserId = userId,
            IsValid = true,
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
        var partnerValidationEvent = new PartnerValidationEvent
        {
            BlobName = partnershipsBlobName,
            Created = DateTime.Now.AddMinutes(7),
            UserId = userId,
            IsValid = true
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>
            {
                companyDetailsAntivirusCheckEvent,
                companyDetailsAntivirusResultEvent,
                registrationValidationEvent,
                partnershipsAntivirusCheckEvent,
                partnershipsAntivirusResultEvent,
                partnerValidationEvent
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
            CompanyDetailsFileIsValid = true,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            PartnershipsFileName = partnershipsAntivirusCheckEvent.FileName,
            PartnershipsDataComplete = true,
            PartnersDataIsValid = true,
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
            UserId = userId,
            IsValid = true,
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
            CompanyDetailsFileIsValid = true,
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
            ValidationPass = true
        });
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task SetValidationEvents_SetsPropertiesCorrectly_WhenLatestUploadedFilesAreNotValidButAPreviousUploadsAre(bool requiresValidation)
    {
        // Arrange
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        const string uploadOneBrandsBlobName = "upload-one-brands-blob-name";
        const string uploadOnePartnershipsBlobName = "upload-one-partnerships-blob-name";
        const string uploadTwoCompanyDetailsBlobName = "upload-two-company-details-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneBrandsFileId = Guid.NewGuid();
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
            UserId = userId,
            RequiresRowValidation = requiresValidation,
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId,
            IsValid = true,
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
            BlobName = uploadOneBrandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(4),
            UserId = userId,
            RequiresRowValidation = requiresValidation,
        };
        var uploadOneBrandValidationEvent = new BrandValidationEvent
        {
            BlobName = uploadOneBrandsBlobName,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId,
            IsValid = true
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
            BlobName = uploadOnePartnershipsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(4),
            UserId = userId,
            RequiresRowValidation = requiresValidation,
        };
        var uploadOnePartnershipsValidationEvent = new PartnerValidationEvent
        {
            BlobName = uploadOnePartnershipsBlobName,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId,
            IsValid = true
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(10),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };
        var uploadTwoCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            BlobName = uploadTwoCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(11),
            UserId = userId,
            RequiresRowValidation = requiresValidation,
        };
        var uploadTwoRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadTwoCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            Created = DateTime.Now.AddMinutes(12),
            UserId = userId,
            IsValid = false,
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
                uploadOneBrandValidationEvent,
                uploadOnePartnershipsAntivirusCheckEvent,
                uploadOnePartnershipsAntivirusResultEvent,
                uploadOnePartnershipsValidationEvent,
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
            RequiresPartnershipsFile = true,
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
                PartnershipsFileName = uploadOnePartnershipsAntivirusCheckEvent.FileName,
                PartnershipsUploadedBy = uploadOnePartnershipsAntivirusCheckEvent.UserId,
                PartnershipsUploadDatetime = uploadOnePartnershipsAntivirusCheckEvent.Created,
            },
            LastSubmittedFiles = null,
            ValidationPass = false,
        });
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task SetValidationEvents_WhenLatestUploadedFilesAreNotValidButAPreviousUploadsAre_WithMultipleValidationEvents_EnsureLatestSuccessfulFileValidationEventsAreUsed(bool requiresValidation)
    {
        // Arrange
        DateTime startTime = DateTime.Parse("01/01/2022 10:00", new CultureInfo("en-GB"));
        const string uploadOneCompanyDetailsBlobName = "upload-one-company-details-blob-name";
        const string uploadOneBrandsBlobName = "upload-one-brands-blob-name";
        const string uploadOnePartnershipsBlobName = "upload-one-partnerships-blob-name";
        const string uploadTwoCompanyDetailsBlobName = "upload-two-company-details-blob-name";
        var userId = Guid.NewGuid();
        var uploadOneCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneBrandsFileId = Guid.NewGuid();
        var uploadOnePartnershipsFileId = Guid.NewGuid();
        var uploadTwoCompanyDetailsFileId = Guid.NewGuid();
        var uploadOneRegistrationSetId = Guid.NewGuid();
        var uploadTwoRegistrationSetId = Guid.NewGuid();
        var uploadOneCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            FileName = "company-details-one.csv",
            FileType = FileType.CompanyDetails,
            Created = startTime,
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneCompanyDetailsFileId,
            BlobName = uploadOneCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = startTime.AddMinutes(1),
            UserId = userId,
            RequiresRowValidation = requiresValidation,
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            Created = startTime.AddMinutes(2),
            UserId = userId,
            IsValid = true,
        };
        var uploadOneBrandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOneBrandsFileId,
            FileName = "brands-one.csv",
            FileType = FileType.Brands,
            Created = startTime.AddMinutes(3),
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOneBrandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOneBrandsFileId,
            BlobName = uploadOneBrandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = startTime.AddMinutes(4),
            UserId = userId,
            RequiresRowValidation = requiresValidation,
        };
        var uploadOneBrandFailedValidationEvent = new BrandValidationEvent
        {
            BlobName = uploadOneBrandsBlobName,
            Created = startTime.AddMinutes(5),
            UserId = userId,
            IsValid = false
        };
        var uploadTwoBrandFailedValidationEvent = new BrandValidationEvent
        {
            BlobName = uploadOneBrandsBlobName,
            Created = startTime.AddMinutes(6),
            UserId = userId,
            IsValid = false
        };
        var latestBrandPassedValidationEvent = new BrandValidationEvent
        {
            BlobName = uploadOneBrandsBlobName,
            Created = startTime.AddMinutes(7),
            UserId = userId,
            IsValid = true
        };
        var uploadOnePartnershipsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadOnePartnershipsFileId,
            FileName = "partnerships-one.csv",
            FileType = FileType.Partnerships,
            Created = startTime.AddMinutes(8),
            UserId = userId,
            RegistrationSetId = uploadOneRegistrationSetId
        };
        var uploadOnePartnershipsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadOnePartnershipsFileId,
            BlobName = uploadOnePartnershipsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = startTime.AddMinutes(9),
            UserId = userId,
            RequiresRowValidation = requiresValidation,
        };
        var uploadOnePartnershipsFailedValidationEvent = new PartnerValidationEvent
        {
            BlobName = uploadOnePartnershipsBlobName,
            Created = startTime.AddMinutes(10),
            UserId = userId,
            IsValid = false
        };
        var uploadTwoPartnershipsFailedValidationEvent = new PartnerValidationEvent
        {
            BlobName = uploadOnePartnershipsBlobName,
            Created = startTime.AddMinutes(11),
            UserId = userId,
            IsValid = false
        };
        var latestPartnershipsPassedValidationEvent = new PartnerValidationEvent
        {
            BlobName = uploadOnePartnershipsBlobName,
            Created = startTime.AddMinutes(12),
            UserId = userId,
            IsValid = true
        };
        var uploadTwoCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            FileName = "company-details-two.csv",
            FileType = FileType.CompanyDetails,
            Created = startTime.AddDays(1).AddMinutes(1),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };
        var uploadTwoCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoCompanyDetailsFileId,
            BlobName = uploadTwoCompanyDetailsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = startTime.AddDays(1).AddMinutes(2),
            UserId = userId,
            RequiresRowValidation = requiresValidation,
        };
        var uploadTwoRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadTwoCompanyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            Created = startTime.AddDays(1).AddMinutes(3),
            UserId = userId,
            IsValid = true,
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
                uploadOneBrandFailedValidationEvent,
                uploadTwoBrandFailedValidationEvent,
                latestBrandPassedValidationEvent,
                uploadOnePartnershipsAntivirusCheckEvent,
                uploadOnePartnershipsAntivirusResultEvent,
                uploadOnePartnershipsFailedValidationEvent,
                uploadTwoPartnershipsFailedValidationEvent,
                latestPartnershipsPassedValidationEvent,
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
            RequiresPartnershipsFile = true,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            CompanyDetailsFileIsValid = true,
            BrandsFileName = null,
            BrandsDataComplete = false,
            BrandsUploadedBy = null,
            BrandsUploadedDate = null,
            BrandsDataIsValid = false,
            PartnershipsFileName = null,
            PartnershipsDataComplete = false,
            PartnershipsUploadedBy = null,
            PartnershipsUploadedDate = null,
            PartnersDataIsValid = false,
            LastUploadedValidFiles = new UploadedRegistrationFilesInformation
            {
                CompanyDetailsFileId = uploadOneCompanyDetailsAntivirusCheckEvent.FileId,
                CompanyDetailsFileName = uploadOneCompanyDetailsAntivirusCheckEvent.FileName,
                CompanyDetailsUploadedBy = uploadOneCompanyDetailsAntivirusCheckEvent.UserId,
                CompanyDetailsUploadDatetime = uploadOneCompanyDetailsAntivirusCheckEvent.Created,
                BrandsFileName = uploadOneBrandsAntivirusCheckEvent.FileName,
                BrandsUploadedBy = uploadOneBrandsAntivirusCheckEvent.UserId,
                BrandsUploadDatetime = uploadOneBrandsAntivirusCheckEvent.Created,
                PartnershipsFileName = uploadOnePartnershipsAntivirusCheckEvent.FileName,
                PartnershipsUploadedBy = uploadOnePartnershipsAntivirusCheckEvent.UserId,
                PartnershipsUploadDatetime = uploadOnePartnershipsAntivirusCheckEvent.Created,
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
            UserId = userId,
            IsValid = true,
            OrganisationMemberCount = 10
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
            UserId = userId,
            IsValid = true,
            OrganisationMemberCount = 10
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
        var uploadTwoBrandValidationEvent = new BrandValidationEvent
        {
            BlobName = uploadTwoBrandsBlobName,
            Created = DateTime.Now.AddMinutes(9),
            UserId = userId,
            IsValid = true
        };
        var uploadTwoPartnershipsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = uploadTwoPartnershipsFileId,
            FileName = "partnerships-two.csv",
            FileType = FileType.Partnerships,
            Created = DateTime.Now.AddMinutes(10),
            UserId = userId,
            RegistrationSetId = uploadTwoRegistrationSetId
        };
        var uploadTwoPartnershipsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = uploadTwoPartnershipsFileId,
            BlobName = uploadTwoPartnershipsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(11),
            UserId = userId
        };
        var uploadTwoPartnerValidationEvent = new PartnerValidationEvent
        {
            BlobName = uploadTwoPartnershipsBlobName,
            Created = DateTime.Now.AddMinutes(12),
            UserId = userId,
            IsValid = true
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
                uploadTwoBrandValidationEvent,
                uploadTwoPartnershipsAntivirusCheckEvent,
                uploadTwoPartnershipsAntivirusResultEvent,
                uploadTwoPartnerValidationEvent,
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            OrganisationMemberCount = 10,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            CompanyDetailsFileIsValid = true,
            BrandsFileName = uploadTwoBrandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsDataIsValid = true,
            BrandsUploadedBy = uploadTwoBrandsAntivirusCheckEvent.UserId,
            BrandsUploadedDate = uploadTwoBrandsAntivirusCheckEvent.Created,
            PartnershipsFileName = uploadTwoPartnershipsAntivirusCheckEvent.FileName,
            PartnershipsDataComplete = true,
            PartnersDataIsValid = true,
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
            UserId = userId,
        };
        var uploadOneRegistrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = uploadOneCompanyDetailsBlobName,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId,
            OrganisationMemberCount = 10,
            IsValid = true,
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
            UserId = userId,
            OrganisationMemberCount = 10,
            IsValid = true,
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
        var uploadTwoBrandValidationEvent = new BrandValidationEvent
        {
            BlobName = uploadTwoBrandsBlobName,
            Created = DateTime.Now.AddMinutes(9),
            UserId = userId,
            IsValid = true
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
                uploadTwoBrandValidationEvent,
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            OrganisationMemberCount = 10,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            CompanyDetailsFileIsValid = true,
            BrandsFileName = uploadTwoBrandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsDataIsValid = true,
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
            UserId = userId,
            IsValid = true,
            OrganisationMemberCount = 10
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
        var uploadOneBrandValidationEvent = new BrandValidationEvent
        {
            BlobName = uploadOneBrandsBlobName,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId,
            IsValid = true
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
            UserId = userId,
            IsValid = true,
            OrganisationMemberCount = 10
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
        var uploadTwoBrandValidationEvent = new BrandValidationEvent
        {
            BlobName = uploadOneBrandsBlobName,
            Created = DateTime.Now.AddMinutes(11),
            UserId = userId,
            IsValid = true
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
                uploadOneBrandValidationEvent,
                uploadOneSubmittedEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent,
                uploadTwoCompanyDetailsAntivirusResultEvent,
                uploadTwoRegistrationValidationEvent,
                uploadTwoBrandsAntivirusCheckEvent,
                uploadTwoBrandsAntivirusResultEvent,
                uploadTwoBrandValidationEvent,
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            OrganisationMemberCount = 10,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            CompanyDetailsFileIsValid = true,
            BrandsFileName = uploadTwoBrandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsDataIsValid = true,
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
            UserId = userId,
            IsValid = true,
            OrganisationMemberCount = 10
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
        var uploadOneBrandValidationEvent = new BrandValidationEvent
        {
            BlobName = uploadOneBrandsBlobName,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId,
            IsValid = true
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
            UserId = userId,
            IsValid = true,
            OrganisationMemberCount = 10
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
        var uploadTwoBrandValidationEvent = new BrandValidationEvent
        {
            BlobName = uploadTwoBrandsBlobName,
            Created = DateTime.Now.AddMinutes(11),
            UserId = userId,
            IsValid = true
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
                uploadOneBrandValidationEvent,
                uploadTwoCompanyDetailsAntivirusCheckEvent,
                uploadTwoCompanyDetailsAntivirusResultEvent,
                uploadTwoRegistrationValidationEvent,
                uploadTwoBrandsAntivirusCheckEvent,
                uploadTwoBrandsAntivirusResultEvent,
                uploadTwoBrandValidationEvent,
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.Should().BeEquivalentTo(new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            OrganisationMemberCount = 10,
            CompanyDetailsFileName = uploadTwoCompanyDetailsAntivirusCheckEvent.FileName,
            CompanyDetailsDataComplete = true,
            CompanyDetailsUploadedBy = uploadTwoCompanyDetailsAntivirusCheckEvent.UserId,
            CompanyDetailsUploadedDate = uploadTwoCompanyDetailsAntivirusCheckEvent.Created,
            CompanyDetailsFileIsValid = true,
            BrandsFileName = uploadTwoBrandsAntivirusCheckEvent.FileName,
            BrandsDataComplete = true,
            BrandsDataIsValid = true,
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
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_WhenSubmittedWithRequiredPartnershipsFile_ValidatePartnershipFileNameIsSet(Guid? registrationSetId)
    {
        // Arrange
        const string companyDetailsBlobName = "upload-company-details-blob-name";
        const string partnershipsFileName = "partnerships.csv";
        const string partnershipsBlobName = "upload-partnership-blob-name";
        var userId = Guid.NewGuid();
        var companyDetailsFileId = Guid.NewGuid();
        var partnershipsFileId = Guid.NewGuid();
        var companyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = companyDetailsFileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = registrationSetId,
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
            UserId = userId,
            IsValid = true,
        };
        var partnershipsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = partnershipsFileId,
            FileName = partnershipsFileName,
            FileType = FileType.Partnerships,
            Created = DateTime.Now.AddMinutes(3),
            UserId = userId,
            RegistrationSetId = registrationSetId,
        };
        var partnershipsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = partnershipsFileId,
            BlobName = partnershipsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(4),
            UserId = userId
        };
        var partnershipsValidationEvent = new PartnerValidationEvent
        {
            BlobName = partnershipsBlobName,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId,
            IsValid = true
        };
        var submittedEvent = new SubmittedEvent
        {
            FileId = companyDetailsFileId,
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
                partnershipsAntivirusResultEvent,
                partnershipsValidationEvent,
                submittedEvent,
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.LastSubmittedFiles.Should().NotBeNull();
        _registrationSubmissionGetResponse.LastSubmittedFiles.PartnersFileName.Should().NotBeNull();
        _registrationSubmissionGetResponse.LastSubmittedFiles.PartnersFileName.Should().Be(partnershipsFileName);
    }

    [TestMethod]
    [DynamicData(nameof(RegistrationSetIdTestParameters), DynamicDataSourceType.Method)]
    public async Task SetValidationEvents_WhenSubmittedWithRequiredBrandsFile_ValidateBrandsFileNameIsSet(Guid? registrationSetId)
    {
        // Arrange
        const string companyDetailsBlobName = "upload-company-details-blob-name";
        const string brandsFileName = "brands.csv";
        const string brandsBlobName = "upload-brands-blob-name";
        var userId = Guid.NewGuid();
        var companyDetailsFileId = Guid.NewGuid();
        var brandsFileId = Guid.NewGuid();
        var companyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = companyDetailsFileId,
            FileName = "company-details.csv",
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now,
            UserId = userId,
            RegistrationSetId = registrationSetId,
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
            UserId = userId,
            IsValid = true,
        };
        var brandsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileId = brandsFileId,
            FileName = brandsFileName,
            FileType = FileType.Brands,
            Created = DateTime.Now.AddMinutes(3),
            UserId = userId,
            RegistrationSetId = registrationSetId,
        };
        var brandsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = brandsFileId,
            BlobName = brandsBlobName,
            AntivirusScanResult = AntivirusScanResult.Success,
            Created = DateTime.Now.AddMinutes(4),
            UserId = userId
        };
        var brandsValidationEvent = new BrandValidationEvent
        {
            BlobName = brandsBlobName,
            Created = DateTime.Now.AddMinutes(5),
            UserId = userId,
            IsValid = true
        };
        var submittedEvent = new SubmittedEvent
        {
            FileId = companyDetailsFileId,
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
                brandsAntivirusResultEvent,
                brandsValidationEvent,
                submittedEvent,
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, true, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.LastSubmittedFiles.Should().NotBeNull();
        _registrationSubmissionGetResponse.LastSubmittedFiles.BrandsFileName.Should().NotBeNull();
        _registrationSubmissionGetResponse.LastSubmittedFiles.BrandsFileName.Should().Be(brandsFileName);
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
    [DataRow(0, 0, DisplayName = "RowErrorCount_WhenZero_SetsToZeroInResponse")]
    [DataRow(10, 10, DisplayName = "RowErrorCount_WhenTen_SetsToTenInResponse")]
    [DataRow(null, 0, DisplayName = "RowErrorCount_WhenNull_SetsToZeroInResponse")]
    public async Task SetValidationEvents_ResponseHasRowErrorCountSet(int? rowErrorCount, int expectedRowErrorCount)
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
            UserId = userId
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
            UserId = userId,
            RowErrorCount = rowErrorCount
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent> { antivirusCheckEvent, antivirusResultEvent, registrationValidationEvent }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.RowErrorCount.Should().Be(expectedRowErrorCount);
    }

    [TestMethod]
    [DataRow(false, false, DisplayName = "HasMaxErrors_WhenIsFalse_SetsToFalseInResponse")]
    [DataRow(true, true, DisplayName = "HasMaxErrors_WhenIsTrue_SetsToTrueInResponse")]
    [DataRow(null, false, DisplayName = "HasMaxErrors_WhenIsNull_SetsToFalseInResponse")]
    public async Task SetValidationEvents_ResponseHasMaxErrorsIsSet(bool hasMaxErrors, bool expectedMaxErrors)
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
            UserId = userId
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
            UserId = userId,
            HasMaxRowErrors = hasMaxErrors
        };

        _submissionEventRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent> { antivirusCheckEvent, antivirusResultEvent, registrationValidationEvent }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.HasMaxRowErrors.Should().Be(expectedMaxErrors);
    }

    [TestMethod]
    public async Task SetValidationEvents_WhenAllFilesAreValid_ButResponseErrorsNotEmpty_ValidationPassIsFalse()
    {
        // Arrange
        Guid? registrationSetId = Guid.NewGuid();
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
            UserId = userId,
            RequiresRowValidation = true,
        };
        var registrationValidationEvent = new RegistrationValidationEvent
        {
            BlobName = companyDetailsBlobName,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            Errors = new List<string> { "802" },
            Created = DateTime.Now.AddMinutes(2),
            UserId = userId,
            IsValid = true,
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
        var brandValidationEvent = new BrandValidationEvent
        {
            BlobName = brandsBlobName,
            Created = DateTime.Now.AddMinutes(-6),
            UserId = userId,
            IsValid = true
        };
        var partnerValidationEvent = new PartnerValidationEvent
        {
            BlobName = partnershipsBlobName,
            Created = DateTime.Now.AddMinutes(-6),
            UserId = userId,
            IsValid = true
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
                partnershipsAntivirusResultEvent,
                brandValidationEvent,
                partnerValidationEvent,
            }.BuildMock);

        // Act
        await _systemUnderTest.SetValidationEvents(_registrationSubmissionGetResponse, false, CancellationToken.None);

        // Assert
        _registrationSubmissionGetResponse.ValidationPass.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(null, false, true)]
    [DataRow(false, false, true)]
    [DataRow(false, true, true)]
    [DataRow(true, false, false)]
    [DataRow(true, true, true)]
    public void IsEventValid_WithRequiresRowValidation_AssertIsValid(bool? requiresRowValidation, bool isValid, bool expectedResult)
    {
        BrandValidationEvent? brandValidationEvent = new() { IsValid = isValid };
        bool result = RegistrationSubmissionEventHelper.IsEventValid(requiresRowValidation, brandValidationEvent);

        result.Should().Be(expectedResult);
    }

    [TestMethod]
    public void IsEventValid_WhenRequiresRowValidation_WithNullValidationEvent_AssertIsNotValid()
    {
        bool result = RegistrationSubmissionEventHelper.IsEventValid(
            requiresRowValidation: true,
            validationEvent: null);

        result.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(false, false, false, true)]
    [DataRow(true, false, false, false)]
    [DataRow(true, true, false, false)]
    [DataRow(true, true, true, true)]
    public void IsBrandsFileValid_WithFlagsSet_VerifyIsValid(bool requiresBrandsFile, bool brandsDataComplete, bool brandsDataIsValid, bool expectedResult)
    {
        var responseModel = new RegistrationSubmissionGetResponse
        {
            RequiresBrandsFile = requiresBrandsFile,
            BrandsDataComplete = brandsDataComplete,
            BrandsDataIsValid = brandsDataIsValid,
        };

        bool isValid = RegistrationSubmissionEventHelper.IsBrandsFileValid(responseModel);

        isValid.Should().Be(expectedResult);
    }

    [TestMethod]
    [DataRow(false, false, false, true)]
    [DataRow(true, false, false, false)]
    [DataRow(true, true, false, false)]
    [DataRow(true, true, true, true)]
    public void IsPartnerFileValid_WithFlagsSet_VerifyIsValid(bool requiresPartnerFile, bool partnersDataComplete, bool partnersDataIsValid, bool expectedResult)
    {
        var responseModel = new RegistrationSubmissionGetResponse
        {
            RequiresPartnershipsFile = requiresPartnerFile,
            PartnershipsDataComplete = partnersDataComplete,
            PartnersDataIsValid = partnersDataIsValid,
        };

        bool isValid = RegistrationSubmissionEventHelper.IsPartnersFileValid(responseModel);

        isValid.Should().Be(expectedResult);
    }

    private static IEnumerable<object[]> RegistrationSetIdTestParameters()
    {
        yield return new object[] { Guid.NewGuid() };
        yield return new object[] { null };
    }
}