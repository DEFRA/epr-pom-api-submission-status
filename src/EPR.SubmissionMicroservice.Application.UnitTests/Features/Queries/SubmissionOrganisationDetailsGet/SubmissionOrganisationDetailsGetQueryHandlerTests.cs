namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionOrganisationDetailsGet;

using Application.Features.Queries.SubmissionOrganisationDetailsGet;
using Data.Enums;
using Data.Repositories.Queries.Interfaces;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using FluentAssertions;
using Moq;

[TestClass]
public class SubmissionOrganisationDetailsGetQueryHandlerTests
{
    private const string BlobContainerName = "TestBlobContainer";
    private const string CompanyDetailsFileName = "CompanyDetails.csv";
    private const string BrandsFileName = "TestBrands.csv";
    private const string PartnersFileName = "TestPartners.csv";
    private const string BrandsBlobName = "bae256ef-90de-4bdf-afdb-6e0815d9f102";
    private const string PartnersBlobName = "c0f81017-d3f9-4664-b9bd-dbe157591f92";
    private const string RegistrationBlobName = "ab3c4f9c-9da4-4cca-b541-2aa84a72cf82";
    private const string SubmissionPeriod = "Jan to Jun 23";
    private const bool RequiresRowValidation = true;

    private readonly Guid _companyDetailsFileId = Guid.NewGuid();
    private readonly Guid _brandsFileId = Guid.NewGuid();
    private readonly Guid _partnersFileId = Guid.NewGuid();
    private readonly Guid _organisationId = Guid.NewGuid();
    private readonly Guid _submissionId = Guid.NewGuid();
    private readonly Guid _registrationSetId = Guid.NewGuid();
    private Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventsQueryRepositoryMock;
    private SubmissionOrganisationDetailsGetQueryHandler _systemUnderTest;

    [TestInitialize]
    public void SetUp()
    {
        _submissionEventsQueryRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();

        _systemUnderTest = new SubmissionOrganisationDetailsGetQueryHandler(
            _submissionEventsQueryRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponse_ForBrandsBlob_When_Registration_Events_Exist()
    {
        // Arrange
        var submissionOrganisationDetailsGetQuery = new SubmissionOrganisationDetailsGetQuery(
            _submissionId,
            BrandsBlobName);

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(
            includeBrandsAntiVirusCheck: true,
            includeBrandsAntiVirusResult: true,
            includeBrandsValidation: true));

        _submissionEventsQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(submissionOrganisationDetailsGetQuery, CancellationToken.None);

        // Assert
        var expectedResult = new SubmissionOrganisationDetailsGetResponse
        {
            BlobName = RegistrationBlobName
        };
        result.Value.Should().BeEquivalentTo(expectedResult);

        _submissionEventsQueryRepositoryMock.Verify(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponse_ForPartnerBlob_When_Registration_Events_Exist()
    {
        // Arrange
        var submissionOrganisationDetailsGetQuery = new SubmissionOrganisationDetailsGetQuery(
            _submissionId,
            PartnersBlobName);

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(
            includeBrandsAntiVirusCheck: true,
            includeBrandsAntiVirusResult: true,
            includeBrandsValidation: true,
            includePartnersAntiVirusCheck: true,
            includePartnersAntiVirusResult: true,
            includePartnersValidation: true));

        _submissionEventsQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(submissionOrganisationDetailsGetQuery, CancellationToken.None);

        // Assert
        var expectedResult = new SubmissionOrganisationDetailsGetResponse
        {
            BlobName = RegistrationBlobName
        };
        result.Value.Should().BeEquivalentTo(expectedResult);

        _submissionEventsQueryRepositoryMock.Verify(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsError_When_No_Brands_Or_Partners_Events()
    {
        // Arrange
        var submissionOrganisationDetailsGetQuery = new SubmissionOrganisationDetailsGetQuery(
            _submissionId,
            BrandsBlobName);

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet());

        _submissionEventsQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(submissionOrganisationDetailsGetQuery, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();

        _submissionEventsQueryRepositoryMock.Verify(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()), Times.Once);
    }

    [TestMethod]
    [DataRow(false, false, false, false, false, false, false, false, false)]
    [DataRow(true, false, false, false, false, false, false, false, false)]
    [DataRow(true, true, false, false, false, false, false, false, false)]
    [DataRow(true, true, true, false, false, false, false, false, false)]
    [DataRow(true, true, true, true, false, false, false, false, false)]
    [DataRow(true, true, true, true, true, false, false, false, false)]
    [DataRow(true, true, true, true, true, true, false, false, false)]
    [DataRow(true, true, true, true, true, true, true, false, false)]
    public async Task Handle_ReturnsError_When_Events_Are_Missing(
        bool includeRegistrationAntiVirusCheck,
        bool includeRegistrationAntiVirusResult,
        bool includeRegistrationValidation,
        bool includeBrandsAntiVirusCheck,
        bool includeBrandsAntiVirusResult,
        bool includeBrandsValidation,
        bool includePartnersAntiVirusCheck,
        bool includePartnersAntiVirusResult,
        bool includePartnersValidation)
    {
        // Arrange
        var submissionOrganisationDetailsGetQuery = new SubmissionOrganisationDetailsGetQuery(
            _submissionId,
            PartnersBlobName);

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(
            includeRegistrationAntiVirusCheck: includeRegistrationAntiVirusCheck,
            includeRegistrationAntiVirusResult: includeRegistrationAntiVirusResult,
            includeRegistrationValidation: includeRegistrationValidation,
            includeBrandsAntiVirusCheck: includeBrandsAntiVirusCheck,
            includeBrandsAntiVirusResult: includeBrandsAntiVirusResult,
            includeBrandsValidation: includeBrandsValidation,
            includePartnersAntiVirusCheck: includePartnersAntiVirusCheck,
            includePartnersAntiVirusResult: includePartnersAntiVirusResult,
            includePartnersValidation: includePartnersValidation));

        _submissionEventsQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(submissionOrganisationDetailsGetQuery, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();

        _submissionEventsQueryRepositoryMock.Verify(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()), Times.Once);
    }

    [TestMethod]
    [DataRow(false, false, false, false, false, false, false, false, false)]
    [DataRow(true, false, false, false, false, false, false, false, false)]
    [DataRow(true, true, false, false, false, false, false, false, false)]
    [DataRow(true, true, true, false, false, false, false, false, false)]
    [DataRow(true, true, true, true, false, false, false, false, false)]
    [DataRow(true, true, true, true, true, false, false, false, false)]
    [DataRow(true, true, true, true, true, true, false, false, false)]
    [DataRow(true, true, true, true, true, true, true, false, false)]
    [DataRow(true, true, true, true, true, true, true, true, false)]
    [DataRow(true, true, true, true, true, true, true, false, true)]
    public async Task Handle_ReturnsError_When_Events_Are_Missing_In_Reverse_Order(
        bool includePartnersValidation,
        bool includePartnersAntiVirusResult,
        bool includePartnersAntiVirusCheck,
        bool includeBrandsValidation,
        bool includeBrandsAntiVirusResult,
        bool includeBrandsAntiVirusCheck,
        bool includeRegistrationValidation,
        bool includeRegistrationAntiVirusResult,
        bool includeRegistrationAntiVirusCheck)
    {
        // Arrange
        var submissionOrganisationDetailsGetQuery = new SubmissionOrganisationDetailsGetQuery(
            _submissionId,
            PartnersBlobName);

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(
            includeRegistrationAntiVirusCheck: includeRegistrationAntiVirusCheck,
            includeRegistrationAntiVirusResult: includeRegistrationAntiVirusResult,
            includeRegistrationValidation: includeRegistrationValidation,
            includeBrandsAntiVirusCheck: includeBrandsAntiVirusCheck,
            includeBrandsAntiVirusResult: includeBrandsAntiVirusResult,
            includeBrandsValidation: includeBrandsValidation,
            includePartnersAntiVirusCheck: includePartnersAntiVirusCheck,
            includePartnersAntiVirusResult: includePartnersAntiVirusResult,
            includePartnersValidation: includePartnersValidation));

        _submissionEventsQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(submissionOrganisationDetailsGetQuery, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();

        _submissionEventsQueryRepositoryMock.Verify(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsError_When_RegistrationBlobName_Is_Null()
    {
        // Arrange
        var submissionOrganisationDetailsGetQuery = new SubmissionOrganisationDetailsGetQuery(
            _submissionId,
            BrandsBlobName);

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(
            includeBrandsAntiVirusCheck: true,
            includeBrandsAntiVirusResult: true,
            includeBrandsValidation: true,
            registrationBlobName: null));

        _submissionEventsQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(submissionOrganisationDetailsGetQuery, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();

        _submissionEventsQueryRepositoryMock.Verify(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsError_When_BlobName_Does_Not_Exist()
    {
        // Arrange
        var submissionOrganisationDetailsGetQuery = new SubmissionOrganisationDetailsGetQuery(
            _submissionId,
            "NonExistentBlob");

        var events = new List<AbstractSubmissionEvent>();
        events.AddRange(CreateEventSet(
            includeBrandsAntiVirusCheck: true,
            includeBrandsAntiVirusResult: true,
            includeBrandsValidation: true));

        _submissionEventsQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(submissionOrganisationDetailsGetQuery, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();

        _submissionEventsQueryRepositoryMock.Verify(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsError_When_No_Submission_Events_Found()
    {
        // Arrange
        var submissionOrganisationDetailsGetQuery = new SubmissionOrganisationDetailsGetQuery(_submissionId, PartnersBlobName);

        _submissionEventsQueryRepositoryMock
            .Setup(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new List<AbstractSubmissionEvent>().BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(submissionOrganisationDetailsGetQuery, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();

        _submissionEventsQueryRepositoryMock.Verify(x => x.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()), Times.Once);
    }

    private IEnumerable<AbstractSubmissionEvent> CreateEventSet(
        bool includeRegistrationAntiVirusCheck = true,
        bool includeRegistrationAntiVirusResult = true,
        bool includeRegistrationValidation = true,
        bool includeBrandsAntiVirusCheck = false,
        bool includeBrandsAntiVirusResult = false,
        bool includeBrandsValidation = false,
        bool includePartnersAntiVirusCheck = false,
        bool includePartnersAntiVirusResult = false,
        bool includePartnersValidation = false,
        DateTime? eventTimestamp = null,
        string registrationBlobName = RegistrationBlobName)
    {
        if (includeRegistrationAntiVirusCheck)
        {
            yield return new AntivirusCheckEvent
            {
                Created = eventTimestamp.GetValueOrDefault(),
                BlobContainerName = BlobContainerName,
                BlobName = null,
                FileId = _companyDetailsFileId,
                FileName = CompanyDetailsFileName,
                FileType = FileType.CompanyDetails,
                RegistrationSetId = _registrationSetId,
                SubmissionId = _submissionId,
            };
        }

        if (includeRegistrationAntiVirusResult)
        {
            yield return new AntivirusResultEvent
            {
                Created = eventTimestamp.GetValueOrDefault().AddMinutes(1),
                BlobContainerName = BlobContainerName,
                BlobName = registrationBlobName,
                FileId = _companyDetailsFileId,
                AntivirusScanResult = AntivirusScanResult.Success,
                RequiresRowValidation = RequiresRowValidation,
                SubmissionId = _submissionId,
            };
        }

        if (includeRegistrationValidation)
        {
            yield return new RegistrationValidationEvent
            {
                Created = eventTimestamp.GetValueOrDefault().AddMinutes(2),
                BlobContainerName = BlobContainerName,
                BlobName = registrationBlobName,
                IsValid = true,
                RequiresBrandsFile = true,
                RequiresPartnershipsFile = true,
                SubmissionId = _submissionId,
            };
        }

        if (includeBrandsAntiVirusCheck)
        {
            yield return new AntivirusCheckEvent
            {
                Created = eventTimestamp.GetValueOrDefault().AddMinutes(4),
                BlobContainerName = BlobContainerName,
                BlobName = null,
                FileId = _brandsFileId,
                FileName = BrandsFileName,
                FileType = FileType.Brands,
                RegistrationSetId = _registrationSetId,
                SubmissionId = _submissionId,
            };
        }

        if (includeBrandsAntiVirusResult)
        {
            yield return new AntivirusResultEvent
            {
                Created = eventTimestamp.GetValueOrDefault().AddMinutes(5),
                BlobContainerName = BlobContainerName,
                BlobName = BrandsBlobName,
                FileId = _brandsFileId,
                AntivirusScanResult = AntivirusScanResult.Success,
                RequiresRowValidation = RequiresRowValidation,
                SubmissionId = _submissionId,
            };
        }

        if (includeBrandsValidation)
        {
            yield return new BrandValidationEvent
            {
                Created = eventTimestamp.GetValueOrDefault().AddMinutes(6),
                BlobContainerName = BlobContainerName,
                BlobName = BrandsBlobName,
                IsValid = true,
                SubmissionId = _submissionId,
            };
        }

        if (includePartnersAntiVirusCheck)
        {
            yield return new AntivirusCheckEvent
            {
                Created = eventTimestamp.GetValueOrDefault().AddMinutes(7),
                BlobContainerName = BlobContainerName,
                BlobName = null,
                FileId = _partnersFileId,
                FileName = PartnersFileName,
                FileType = FileType.Partnerships,
                RegistrationSetId = _registrationSetId,
                SubmissionId = _submissionId,
            };
        }

        if (includePartnersAntiVirusResult)
        {
            yield return new AntivirusResultEvent
            {
                Created = eventTimestamp.GetValueOrDefault().AddMinutes(8),
                BlobContainerName = BlobContainerName,
                BlobName = PartnersBlobName,
                FileId = _partnersFileId,
                AntivirusScanResult = AntivirusScanResult.Success,
                RequiresRowValidation = RequiresRowValidation,
                SubmissionId = _submissionId,
            };
        }

        if (includePartnersValidation)
        {
            yield return new PartnerValidationEvent
            {
                Created = eventTimestamp.GetValueOrDefault().AddMinutes(9),
                BlobContainerName = BlobContainerName,
                BlobName = PartnersBlobName,
                IsValid = true,
                SubmissionId = _submissionId,
            };
        }
    }
}