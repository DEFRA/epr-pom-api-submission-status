using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;
using EPR.SubmissionMicroservice.Application.Options;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.GetRegistrationApplicationDetails;

[TestClass]
public class GetRegistrationApplicationDetailsQueryHandlerTests
{
    private readonly Mock<IQueryRepository<Submission>> _submissionQueryRepositoryMock;
    private readonly Mock<ILogger<GetRegistrationApplicationDetailsQueryHandler>> _loggerMock;
    private readonly Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventQueryRepositoryMock;
    private readonly GetRegistrationApplicationDetailsQueryHandler _handler;

    public GetRegistrationApplicationDetailsQueryHandlerTests()
    {
        var featureFlagOption = new FeatureFlagOptions { IsQueryLateFeeEnabled = true };
        _submissionQueryRepositoryMock = new Mock<IQueryRepository<Submission>>();
        _submissionEventQueryRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _loggerMock = new Mock<ILogger<GetRegistrationApplicationDetailsQueryHandler>>();
        _handler = new GetRegistrationApplicationDetailsQueryHandler(
            _submissionQueryRepositoryMock.Object,
            _submissionEventQueryRepositoryMock.Object,
            Microsoft.Extensions.Options.Options.Create(featureFlagOption),
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task Handle_ShouldReturnNull_WhenNoSubmissionIsFound()
    {
        // Arrange
        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(Enumerable.Empty<Submission>().BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_ShouldReturnNullFields_WhenNoEventsAssociated()
    {
        // Arrange
        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1"
        };

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(Enumerable.Empty<AbstractSubmissionEvent>().BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.IsSubmitted.Should().BeFalse();
        result.Value.ApplicationReferenceNumber.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_ShouldReturnSubmittedToRegulator_WhenSubmitted()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = fileId
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now,
            FileId = fileId,
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile!.SubmittedByName.Should().Be("User1");
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnFileUploaded_WhenBrandFileIsRequired_And_BrandFile_Is_Uploaded()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var regFileId = Guid.NewGuid();
        var brandFileId = Guid.NewGuid();
        var partnerFileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = false,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = regFileId
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = regFileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var latestBrandAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Brands,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = brandFileId
        };

        var latestBranAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = brandFileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var brandValidationEvent = new BrandValidationEvent
        {
            ErrorCount = 0,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var latestPartnerAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Partnerships,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = partnerFileId
        };

        var latestPartnerAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = partnerFileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var partnerValidationEvent = new PartnerValidationEvent
        {
            ErrorCount = 0,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                latestBrandAntivirusCheckEvent,
                latestBranAntivirusResultEvent,
                brandValidationEvent,
                latestPartnerAntivirusCheckEvent,
                latestPartnerAntivirusResultEvent,
                partnerValidationEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.ApplicationStatus.ToString().Should().Be("FileUploaded");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnFileUploaded_WhenBrandFileIsRequired_And_BrandFile_Has_validation_Errors()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var regFileId = Guid.NewGuid();
        var brandFileId = Guid.NewGuid();
        var partnerFileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = false,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = regFileId
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = regFileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var latestBrandAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Brands,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = brandFileId
        };

        var latestBranAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = brandFileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var brandValidationEvent = new BrandValidationEvent
        {
            ErrorCount = 2,
            IsValid = false,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var latestPartnerAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Partnerships,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = partnerFileId
        };

        var latestPartnerAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = partnerFileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var partnerValidationEvent = new PartnerValidationEvent
        {
            ErrorCount = 0,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                latestBrandAntivirusCheckEvent,
                latestBranAntivirusResultEvent,
                brandValidationEvent,
                latestPartnerAntivirusCheckEvent,
                latestPartnerAntivirusResultEvent,
                partnerValidationEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.ApplicationStatus.ToString().Should().Be("NotStarted");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnFileUploaded_WhenPartnerFileIsRequired_And_PartnerFile_Has_validation_Errors()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var regFileId = Guid.NewGuid();
        var brandFileId = Guid.NewGuid();
        var partnerFileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = false,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = regFileId
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = regFileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var latestBrandAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Brands,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = brandFileId
        };

        var latestBranAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = brandFileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var brandValidationEvent = new BrandValidationEvent
        {
            ErrorCount = 0,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var latestPartnerAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Partnerships,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = partnerFileId
        };

        var latestPartnerAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = partnerFileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var partnerValidationEvent = new PartnerValidationEvent
        {
            ErrorCount = 2,
            IsValid = false,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                latestBrandAntivirusCheckEvent,
                latestBranAntivirusResultEvent,
                brandValidationEvent,
                latestPartnerAntivirusCheckEvent,
                latestPartnerAntivirusResultEvent,
                partnerValidationEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.ApplicationStatus.ToString().Should().Be("NotStarted");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnNotStarted_WhenBrandFileIsRequired_And_BrandFile_Not_Uploaded()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = false,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = fileId
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.ApplicationStatus.ToString().Should().Be("NotStarted");
    }

    [TestMethod]
    public async Task WhenRegistrationValidationEvent_IsNull_Handle_ShouldReturnSubmittedAndHasRecentFileUpload_WhenSubmitted()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = fileId
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        RegistrationValidationEvent registrationValidationEvent = null;

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now,
            FileId = fileId,
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile.FileId.Should().Be(submissionEvent.FileId);
        result.Value.LastSubmittedFile.SubmittedByName.Should().Be(submissionEvent.SubmittedBy);
        result.Value.LastSubmittedFile.SubmittedDateTime.Should().Be(submissionEvent.Created);
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedAndHasRecentFileUpload");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnSubmittedAndHasRecentFileUpload_WhenSubmittedButHasNewFileUploaded()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = fileId
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now,
            FileId = fileId,
            SubmittedBy = "User1"
        };

        var latestCompanyDetailsAntivirusCheckEvent2 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(1),
            FileId = fileId
        };

        var latestCompanyDetailsAntivirusResultEvent2 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(1)
        };

        var registrationValidationEvent2 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(1)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                submissionEvent,
                registrationValidationEvent2,
                latestCompanyDetailsAntivirusCheckEvent2,
                latestCompanyDetailsAntivirusResultEvent2,
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedAndHasRecentFileUpload");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnFileUploaded_WhenNotSubmittedButFileUploaded()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = false,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = fileId
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("FileUploaded");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnNotStarted_WhenNotSubmittedAndFileUploadedIsNotValid()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = false,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = fileId
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 1,
            WarningCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("NotStarted");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnCorrectResponse_WhenSubmissionAndEventsFound()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = fileId
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var latestBrandAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Brands,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-4),
            FileId = fileId
        };

        var latestBrandAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-4)
        };

        var latestBrandValidationEvent = new BrandValidationEvent
        {
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-4),
            ErrorCount = 0,
            WarningCount = 0,
            IsValid = true
        };

        var latestPartnershipAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Partnerships,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-3),
            FileId = fileId
        };

        var latestPartnershipAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-3)
        };

        var latestPartnershipValidationEvent = new PartnerValidationEvent
        {
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-3),
            IsValid = true,
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now,
            FileId = fileId,
            SubmittedBy = "User1"
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(2),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(2)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(3),
            Decision = RegulatorDecision.Approved,
            DecisionDate = DateTime.Now.AddMinutes(3),
            RegistrationReferenceNumber = "TestRef"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestPartnershipAntivirusResultEvent,
                latestBrandAntivirusCheckEvent,
                latestBrandAntivirusResultEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                latestPartnershipAntivirusCheckEvent,
                latestBrandValidationEvent,
                latestPartnershipValidationEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile!.SubmittedByName.Should().Be("User1");
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.RegistrationFeePaymentMethod!.Should().Be("PayByPhone");
        result.Value.RegistrationApplicationSubmitted.Should().BeTrue();
        result.Value.ApplicationStatus.ToString().Should().Be("ApprovedByRegulator");
        result.Value.RegistrationReferenceNumber.Should().Be("TestRef");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnCorrectResponse_WhenPaymentAndApplicationSubmittedEventsFound()
    {
        // Arrange
        var applicationReferenceNumber = "TestRef";
        var fileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1"
        };

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-3),
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-2)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-1)
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now,
            FileId = fileId,
            SubmittedBy = "User1"
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(2),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(2)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { latestCompanyDetailsAntivirusCheckEvent, latestCompanyDetailsAntivirusResultEvent, registrationValidationEvent, submissionEvent, feePaymentEvent, applicationSubmittedEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile!.SubmittedByName.Should().Be("User1");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnCorrectStatus_WhenRequiredFilesNotUploaded()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1"
        };

        var submission = new Submission
        {
            Id = submissionId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
        };

        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            Created = DateTime.Now.AddMinutes(-10),
            RegistrationSetId = Guid.NewGuid(),
            SubmissionId = submissionId,
            FileId = fileId
        };

        var antivirusResultEvent = new AntivirusResultEvent
        {
            Created = DateTime.Now.AddMinutes(-10),
            FileId = fileId,
            SubmissionId = submissionId,
            AntivirusScanResult = AntivirusScanResult.Success
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 5,
            WarningCount = 5,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = false
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { antivirusCheckEvent, antivirusResultEvent, registrationValidationEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.ApplicationStatus.Should().Be(GetRegistrationApplicationDetailsResponse.ApplicationStatusType.NotStarted);
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Accepted)]
    [DataRow(RegulatorDecision.Approved)]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task Handle_ShouldSetStatusToRegulator_WhenRegulatorDecisionExists(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var createdDate = DateTime.Now;

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1"
        };

        var submission = new Submission
        {
            Id = submissionId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = createdDate,
            IsSubmitted = true
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = createdDate,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = createdDate
        };

        var submissionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = createdDate,
            Decision = decision,
            DecisionDate = createdDate
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submissionEvent, latestCompanyDetailsAntivirusCheckEvent, latestCompanyDetailsAntivirusResultEvent, registrationValidationEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.ApplicationStatus.ToString().Should().Be($"{decision.ToString()}ByRegulator");
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Accepted)]
    [DataRow(RegulatorDecision.Approved)]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task Handle_ShouldIgnoreRegulatorDecision_WhenNewSubmissionExists(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var createdDate = DateTime.Now;
        var submissionAfterDecision = decision is RegulatorDecision.Cancelled or RegulatorDecision.Queried or RegulatorDecision.Rejected;

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1"
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = submissionAfterDecision ? createdDate.AddMinutes(5) : createdDate,
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = submissionAfterDecision ? createdDate.AddMinutes(5) : createdDate,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = submissionAfterDecision ? createdDate.AddMinutes(5) : createdDate,
        };

        var submission = new Submission
        {
            Id = submissionId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = submissionAfterDecision ? createdDate.AddMinutes(5) : createdDate,
            IsSubmitted = true
        };

        var decisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = createdDate,
            Decision = decision,
            DecisionDate = createdDate
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { decisionEvent, latestCompanyDetailsAntivirusCheckEvent, latestCompanyDetailsAntivirusResultEvent, registrationValidationEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.ApplicationStatus.ToString().Should().Be(
            submissionAfterDecision
                ? "SubmittedAndHasRecentFileUpload"
                : $"{decision.ToString()}ByRegulator");

        result.Value.RegistrationFeePaymentMethod.Should().BeNull();
        result.Value.RegistrationApplicationSubmittedComment.Should().BeNull();
        result.Value.RegistrationApplicationSubmittedDate.Should().BeNull();
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task Handle_ShouldIgnorePreviousViewPaymentAndApplicationSubmitted_WhenRegulatorRejectedOrQueriedOrCancelled(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var createdDate = DateTime.Now;

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1"
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = createdDate,
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = createdDate,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = createdDate,
        };

        var submission = new Submission
        {
            Id = submissionId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = createdDate,
            IsSubmitted = true,
            AppReferenceNumber = "TestAppref123"
        };

        var decisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submissionId,
            Created = createdDate.AddMinutes(-1),
            Decision = decision,
            DecisionDate = createdDate
        };

        var viewPayment = new RegistrationFeePaymentEvent
        {
            SubmissionId = submissionId,
            Created = createdDate.AddMinutes(-5),
            ApplicationReferenceNumber = submission.AppReferenceNumber,
            PaymentMethod = "PayOnline",
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            ApplicationReferenceNumber = submission.AppReferenceNumber,
            SubmissionDate = DateTime.Now,
            Comments = "Test comments",
            Created = createdDate.AddMinutes(-5)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { applicationSubmittedEvent, viewPayment, decisionEvent, latestCompanyDetailsAntivirusCheckEvent, latestCompanyDetailsAntivirusResultEvent, registrationValidationEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedAndHasRecentFileUpload");
        result.Value.RegistrationFeePaymentMethod.Should().BeNull();
        result.Value.RegistrationApplicationSubmittedComment.Should().BeNull();
        result.Value.RegistrationApplicationSubmittedDate.Should().BeNull();
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task Handle_ShouldNOTIgnoreViewPaymentAndApplicationSubmitted_WhenNewEventIsAfterRegulatorDecision(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var createdDate = DateTime.Now;

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1"
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = createdDate,
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = createdDate,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = createdDate,
        };

        var submission = new Submission
        {
            Id = submissionId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = createdDate,
            IsSubmitted = true,
            AppReferenceNumber = "TestAppref123"
        };

        var decisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submissionId,
            Created = createdDate.AddMinutes(-1),
            Decision = decision,
            DecisionDate = createdDate
        };

        var viewPayment = new RegistrationFeePaymentEvent
        {
            SubmissionId = submissionId,
            Created = createdDate.AddMinutes(5),
            ApplicationReferenceNumber = submission.AppReferenceNumber,
            PaymentMethod = "PayOnline",
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            ApplicationReferenceNumber = submission.AppReferenceNumber,
            SubmissionDate = new DateTime(2025, 1, 1),
            Comments = "Test comments",
            Created = createdDate.AddMinutes(5)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { applicationSubmittedEvent, viewPayment, decisionEvent, latestCompanyDetailsAntivirusCheckEvent, latestCompanyDetailsAntivirusResultEvent, registrationValidationEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedAndHasRecentFileUpload");
        result.Value.RegistrationFeePaymentMethod.Should().Be("PayOnline");
        result.Value.RegistrationApplicationSubmittedComment.Should().Be("Test comments");
        result.Value.RegistrationApplicationSubmittedDate.Should().BeCloseTo(new DateTime(2025, 1, 1), TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task Handle_ShouldSetLastSubmittedFileDetails_WhenLatestFileIsNotSubmitted_And_Previous_Decision_Exists(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(-5)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            Decision = decision,
            DecisionDate = DateTime.Now.AddMinutes(-5),
            RegistrationReferenceNumber = "TestRef"
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile.SubmittedByName.Should().Be(submissionEvent.SubmittedBy);
        result.Value.LastSubmittedFile.SubmittedDateTime.Should().Be(submissionEvent.Created);
        result.Value.LastSubmittedFile.FileId.Should().Be(submissionEvent.FileId);
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.RegistrationFeePaymentMethod!.Should().BeNull();
        result.Value.RegistrationApplicationSubmitted.Should().BeFalse();
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedAndHasRecentFileUpload");
        result.Value.RegistrationReferenceNumber.Should().BeNull();
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task Handle_ShouldSetApplicationStatus_WhenLatestFileIsSubmitted(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(-5)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            Decision = decision,
            DecisionDate = DateTime.Now.AddMinutes(-5),
            RegistrationReferenceNumber = "TestRef"
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            FileId = fileId,
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent,
                submissionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.RegistrationFeePaymentMethod!.Should().BeNull();
        result.Value.RegistrationApplicationSubmitted.Should().BeFalse();
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
        result.Value.RegistrationReferenceNumber.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_ReSubmission_ShouldSetApplicationStatus_WhenNewFileExists()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";
        var previousCreated = DateTime.Now.AddMinutes(-1);
        var latestCreated = DateTime.Now.AddMinutes(+1);

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = previousCreated,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent1 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = previousCreated,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent1 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = previousCreated,
        };

        var registrationValidationEvent1 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = previousCreated
        };

        var feePaymentEvent1 = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent1 = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = previousCreated
        };

        var submissionEvent1 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationDecisionEvent1 = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now,
            Decision = RegulatorDecision.Approved,
            DecisionDate = DateTime.Now,
            RegistrationReferenceNumber = "TestRef"
        };

        var registrationValidationEvent2 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = latestCreated
        };

        var latestCompanyDetailsAntivirusCheckEvent2 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = latestCreated,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent2 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = latestCreated
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                latestCompanyDetailsAntivirusCheckEvent1,
                latestCompanyDetailsAntivirusResultEvent1,
                registrationValidationEvent1,
                submissionEvent1,
                registrationDecisionEvent1,
                feePaymentEvent1,
                applicationSubmittedEvent1,
                latestCompanyDetailsAntivirusCheckEvent2,
                latestCompanyDetailsAntivirusResultEvent2,
                registrationValidationEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedAndHasRecentFileUpload");
        result.Value.RegistrationFeePaymentMethod.Should().BeNull();
        result.Value.RegistrationApplicationSubmittedComment.Should().BeNull();
        result.Value.RegistrationApplicationSubmittedDate.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_ReSubmission_ShouldSetApplicationStatus_WhenNewFileSubmitted()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";
        var previousCreated = DateTime.Now.AddMinutes(-1);
        var latestCreated = DateTime.Now.AddMinutes(+1);

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = previousCreated,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent1 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = previousCreated,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent1 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = previousCreated
        };

        var registrationValidationEvent1 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = previousCreated
        };

        var feePaymentEvent1 = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent1 = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = previousCreated
        };

        var submissionEvent1 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationDecisionEvent1 = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now,
            Decision = RegulatorDecision.Approved,
            DecisionDate = DateTime.Now,
            RegistrationReferenceNumber = "TestRef"
        };

        var registrationValidationEvent2 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = latestCreated
        };

        var latestCompanyDetailsAntivirusCheckEvent2 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = latestCreated,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent2 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = latestCreated
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = latestCreated.AddMinutes(+1),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                latestCompanyDetailsAntivirusCheckEvent1,
                latestCompanyDetailsAntivirusResultEvent1,
                registrationValidationEvent1,
                submissionEvent1,
                registrationDecisionEvent1,
                feePaymentEvent1,
                applicationSubmittedEvent1,
                latestCompanyDetailsAntivirusCheckEvent2,
                latestCompanyDetailsAntivirusResultEvent2,
                registrationValidationEvent2,
                submissionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
        result.Value.RegistrationFeePaymentMethod.Should().BeNull();
        result.Value.RegistrationApplicationSubmittedComment.Should().BeNull();
        result.Value.RegistrationApplicationSubmittedDate.Should().BeNull();
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Accepted)]
    [DataRow(RegulatorDecision.Approved)]
    public async Task Handle_ReSubmission_ShouldSetRegistrationReferenceNumber_From_Previous_Successful_Submission_Current_Submission_Not_Approved_Yet(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";
        var previousCreated = DateTime.Now.AddMinutes(-1);
        var latestCreated = DateTime.Now.AddMinutes(+1);

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = previousCreated,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent1 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = previousCreated,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent1 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = previousCreated
        };

        var registrationValidationEvent1 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = previousCreated
        };

        var feePaymentEvent1 = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent1 = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = previousCreated
        };

        var submissionEvent1 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationDecisionEvent1 = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = latestCreated,
            Decision = decision,
            DecisionDate = latestCreated,
            RegistrationReferenceNumber = "TestRefOld"
        };

        var registrationValidationEvent2 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = latestCreated
        };

        var latestCompanyDetailsAntivirusCheckEvent2 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = latestCreated,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent2 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = latestCreated
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = latestCreated,
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                latestCompanyDetailsAntivirusCheckEvent1,
                latestCompanyDetailsAntivirusResultEvent1,
                registrationValidationEvent1,
                submissionEvent1,
                registrationDecisionEvent1,
                feePaymentEvent1,
                applicationSubmittedEvent1,
                latestCompanyDetailsAntivirusCheckEvent2,
                latestCompanyDetailsAntivirusResultEvent2,
                registrationValidationEvent2,
                submissionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.RegistrationReferenceNumber.Should().Be("TestRefOld");
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Accepted)]
    [DataRow(RegulatorDecision.Approved)]
    public async Task Handle_ReSubmission_ShouldSetRegistrationReferenceNumber_From_Previous_Successful_Submission_Current_Submission_Rejected(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";
        var previousCreated = DateTime.Now.AddMinutes(-1);
        var latestCreated = DateTime.Now.AddMinutes(+1);

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = previousCreated,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent1 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = previousCreated,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent1 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = previousCreated
        };

        var registrationValidationEvent1 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = previousCreated
        };

        var feePaymentEvent1 = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent1 = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = previousCreated
        };

        var submissionEvent1 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationDecisionEvent1 = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = latestCreated,
            Decision = decision,
            DecisionDate = latestCreated,
            RegistrationReferenceNumber = "TestRefOld"
        };

        var registrationValidationEvent2 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = latestCreated
        };

        var latestCompanyDetailsAntivirusCheckEvent2 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = latestCreated,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent2 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = latestCreated
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = latestCreated,
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationDecisionEvent2 = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = latestCreated,
            Decision = RegulatorDecision.Rejected,
            DecisionDate = latestCreated,
            RegistrationReferenceNumber = "TestRefOld"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                latestCompanyDetailsAntivirusCheckEvent1,
                latestCompanyDetailsAntivirusResultEvent1,
                registrationValidationEvent1,
                submissionEvent1,
                registrationDecisionEvent1,
                feePaymentEvent1,
                applicationSubmittedEvent1,
                latestCompanyDetailsAntivirusCheckEvent2,
                latestCompanyDetailsAntivirusResultEvent2,
                registrationValidationEvent2,
                submissionEvent2,
                registrationDecisionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.RegistrationReferenceNumber.Should().Be("TestRefOld");
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Accepted)]
    [DataRow(RegulatorDecision.Approved)]
    public async Task Handle_ReSubmission_ShouldSetRegistrationReferenceNumber_From_Previous_Successful_Submission_Current_Submission_Approved(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";
        var previousCreated = DateTime.Now.AddMinutes(-1);
        var latestCreated = DateTime.Now.AddMinutes(+1);

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = previousCreated,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent1 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = previousCreated,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent1 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = previousCreated
        };

        var registrationValidationEvent1 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = previousCreated
        };

        var feePaymentEvent1 = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent1 = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = previousCreated
        };

        var submissionEvent1 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationDecisionEvent1 = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = latestCreated,
            Decision = decision,
            DecisionDate = latestCreated,
            RegistrationReferenceNumber = "TestRefOld"
        };

        var registrationValidationEvent2 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = latestCreated
        };

        var latestCompanyDetailsAntivirusCheckEvent2 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = latestCreated,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent2 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = latestCreated
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = latestCreated,
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationDecisionEvent2 = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = latestCreated,
            Decision = RegulatorDecision.Approved,
            DecisionDate = latestCreated,
            RegistrationReferenceNumber = "TestRefOld"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                latestCompanyDetailsAntivirusCheckEvent1,
                latestCompanyDetailsAntivirusResultEvent1,
                registrationValidationEvent1,
                submissionEvent1,
                registrationDecisionEvent1,
                feePaymentEvent1,
                applicationSubmittedEvent1,
                latestCompanyDetailsAntivirusCheckEvent2,
                latestCompanyDetailsAntivirusResultEvent2,
                registrationValidationEvent2,
                submissionEvent2,
                registrationDecisionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.RegistrationReferenceNumber.Should().Be("TestRefOld");
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task Handle_ReSubmission_Should_NOT_SetRegistrationReferenceNumber_From_Previous_Unsuccessful_Submission(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";
        var previousCreated = DateTime.Now.AddMinutes(-1);
        var latestCreated = DateTime.Now.AddMinutes(+1);

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = previousCreated,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent1 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = previousCreated,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent1 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = previousCreated
        };

        var registrationValidationEvent1 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = previousCreated
        };

        var feePaymentEvent1 = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent1 = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = previousCreated
        };

        var submissionEvent1 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationDecisionEvent1 = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = previousCreated,
            Decision = decision,
            DecisionDate = previousCreated,
            RegistrationReferenceNumber = "TestRef"
        };

        var registrationValidationEvent2 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = latestCreated
        };

        var latestCompanyDetailsAntivirusCheckEvent2 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = latestCreated,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent2 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = latestCreated
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = latestCreated.AddMinutes(+1),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                latestCompanyDetailsAntivirusCheckEvent1,
                latestCompanyDetailsAntivirusResultEvent1,
                registrationValidationEvent1,
                submissionEvent1,
                registrationDecisionEvent1,
                feePaymentEvent1,
                applicationSubmittedEvent1,
                latestCompanyDetailsAntivirusCheckEvent2,
                latestCompanyDetailsAntivirusResultEvent2,
                registrationValidationEvent2,
                submissionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.RegistrationReferenceNumber.Should().BeNull();
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task Handle_ShouldSetViewPaymentAndApplicationSubmittedFromLatest_WhenNewEventsAreAfterRegulatorDecision(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now.AddMinutes(-5),
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-4),
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-4),
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-4)
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-3),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-2),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-1),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(-5)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now,
            Decision = decision,
            DecisionDate = DateTime.Now,
            RegistrationReferenceNumber = "TestRef"
        };

        var latestCompanyDetailsAntivirusCheckEvent2 = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(1),
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent2 = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(1),
        };

        var registrationValidationEvent2 = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(1)
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(2),
            FileId = fileId,
            SubmittedBy = "User1"
        };

        var feePaymentEvent2 = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(3),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayOnline"
        };

        var applicationSubmittedEvent2 = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(4),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(4)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                registrationValidationEvent,
                submissionEvent,
                feePaymentEvent,
                applicationSubmittedEvent,
                registrationDecisionEvent,
                latestCompanyDetailsAntivirusCheckEvent2,
                latestCompanyDetailsAntivirusResultEvent2,
                registrationValidationEvent2,
                submissionEvent2,
                feePaymentEvent2,
                applicationSubmittedEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.ApplicationReferenceNumber!.Should().Be("TestRef");
        result.Value.RegistrationFeePaymentMethod!.Should().Be("PayOnline");
        result.Value.RegistrationApplicationSubmitted.Should().BeTrue();
        result.Value.RegistrationApplicationSubmittedDate.Should().BeCloseTo(DateTime.Now.AddMinutes(4), TimeSpan.FromSeconds(5));
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
        result.Value.RegistrationReferenceNumber.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_ShouldHandleValidationErrorsCorrectly_WhenErrorsExist()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1"
        };

        var submission = new Submission
        {
            Id = submissionId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 3,
            WarningCount = 3,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = false,
            SubmissionId = submissionId,
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { registrationValidationEvent, latestCompanyDetailsAntivirusCheckEvent, latestCompanyDetailsAntivirusResultEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.ApplicationStatus.Should().Be(GetRegistrationApplicationDetailsResponse.ApplicationStatusType.NotStarted);
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task WithErrorAndWarning_Handle_ShouldSetApplicationStatus_WhenLatestFileIsSubmitted(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(-5)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            Decision = decision,
            DecisionDate = DateTime.Now.AddMinutes(-5),
            RegistrationReferenceNumber = "TestRef"
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 1,
            WarningCount = 1,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            FileId = fileId,
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent,
                submissionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile.FileId.Should().Be(submissionEvent2.FileId);
        result.Value.LastSubmittedFile.SubmittedByName.Should().Be(submissionEvent2.SubmittedBy);
        result.Value.LastSubmittedFile.SubmittedDateTime.Should().Be(submissionEvent2.Created);
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.RegistrationFeePaymentMethod!.Should().NotBeNull();
        result.Value.RegistrationApplicationSubmitted.Should().BeTrue();
        result.Value.ApplicationStatus.ToString().Should().NotBeNull();
        result.Value.RegistrationReferenceNumber.Should().BeNull();
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task WithErrorAndWarningWithFalseFlags_Handle_ShouldSetApplicationStatus_WhenLatestFileIsSubmitted(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(-5)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            Decision = decision,
            DecisionDate = DateTime.Now.AddMinutes(-5),
            RegistrationReferenceNumber = "TestRef"
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 1,
            WarningCount = 1,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = false,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            FileId = fileId,
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent,
                submissionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile.FileId.Should().Be(submissionEvent2.FileId);
        result.Value.LastSubmittedFile.SubmittedByName.Should().Be(submissionEvent2.SubmittedBy);
        result.Value.LastSubmittedFile.SubmittedDateTime.Should().Be(submissionEvent2.Created);
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.RegistrationFeePaymentMethod!.Should().NotBeNull();
        result.Value.RegistrationApplicationSubmitted.Should().BeTrue();
        result.Value.ApplicationStatus.ToString().Should().NotBeNull();
        result.Value.RegistrationReferenceNumber.Should().BeNull();
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task WithWarningAndRequiresPartnershipsFile_IsTrue_Handle_ShouldSetApplicationStatus_WhenLatestFileIsSubmitted(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(-5)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            Decision = decision,
            DecisionDate = DateTime.Now.AddMinutes(-5),
            RegistrationReferenceNumber = "TestRef"
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 1,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = true,
            IsValid = false,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            FileId = fileId,
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent,
                submissionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile.FileId.Should().Be(submissionEvent2.FileId);
        result.Value.LastSubmittedFile.SubmittedByName.Should().Be(submissionEvent2.SubmittedBy);
        result.Value.LastSubmittedFile.SubmittedDateTime.Should().Be(submissionEvent2.Created);
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.RegistrationFeePaymentMethod!.Should().NotBeNull();
        result.Value.RegistrationApplicationSubmitted.Should().BeTrue();
        result.Value.ApplicationStatus.ToString().Should().NotBeNull();
        result.Value.RegistrationReferenceNumber.Should().BeNull();
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task WithWarningAndRequiresBrandsFile_IsTrue_Handle_ShouldSetApplicationStatus_WhenLatestFileIsSubmitted(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(-5)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            Decision = decision,
            DecisionDate = DateTime.Now.AddMinutes(-5),
            RegistrationReferenceNumber = "TestRef"
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 1,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            IsValid = false,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            FileId = fileId,
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent,
                submissionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile.FileId.Should().Be(submissionEvent2.FileId);
        result.Value.LastSubmittedFile.SubmittedByName.Should().Be(submissionEvent2.SubmittedBy);
        result.Value.LastSubmittedFile.SubmittedDateTime.Should().Be(submissionEvent2.Created);
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.RegistrationFeePaymentMethod!.Should().NotBeNull();
        result.Value.RegistrationApplicationSubmitted.Should().BeTrue();
        result.Value.ApplicationStatus.ToString().Should().NotBeNull();
        result.Value.RegistrationReferenceNumber.Should().BeNull();
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.Rejected)]
    public async Task WithNoWarningNoErrorAndRequiresBrandsFile_IsTrue_Handle_ShouldSetApplicationStatus_WhenLatestFileIsSubmitted(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(-5)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            Decision = decision,
            DecisionDate = DateTime.Now.AddMinutes(-5),
            RegistrationReferenceNumber = "TestRef"
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = false,
            IsValid = false,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            FileId = fileId,
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent,
                submissionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile.FileId.Should().Be(submissionEvent2.FileId);
        result.Value.LastSubmittedFile.SubmittedByName.Should().Be(submissionEvent2.SubmittedBy);
        result.Value.LastSubmittedFile.SubmittedDateTime.Should().Be(submissionEvent2.Created);
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.RegistrationFeePaymentMethod!.Should().NotBeNull();
        result.Value.RegistrationApplicationSubmitted.Should().BeTrue();
        result.Value.ApplicationStatus.ToString().Should().NotBeNull();
        result.Value.RegistrationReferenceNumber.Should().BeNull();
    }

    [TestMethod]
    public async Task LatestBrandValidationEvent_WithWarning_Handle_ShouldReturnCorrectResponse_WhenSubmissionAndEventsFound()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = fileId
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var latestBrandAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Brands,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-4),
            FileId = fileId
        };

        var latestBrandAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-4)
        };

        var latestBrandValidationEvent = new BrandValidationEvent
        {
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-4),
            ErrorCount = 0,
            WarningCount = 1,
            IsValid = true
        };

        var latestPartnershipAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Partnerships,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-3),
            FileId = fileId
        };

        var latestPartnershipAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-3)
        };

        var latestPartnershipValidationEvent = new PartnerValidationEvent
        {
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-3),
            IsValid = true,
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now,
            FileId = fileId,
            SubmittedBy = "User1"
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(2),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(2)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(3),
            Decision = RegulatorDecision.Approved,
            DecisionDate = DateTime.Now.AddMinutes(3),
            RegistrationReferenceNumber = "TestRef"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestPartnershipAntivirusResultEvent,
                latestBrandAntivirusCheckEvent,
                latestBrandAntivirusResultEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                latestPartnershipAntivirusCheckEvent,
                latestBrandValidationEvent,
                latestPartnershipValidationEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile!.SubmittedByName.Should().Be("User1");
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.RegistrationFeePaymentMethod!.Should().Be("PayByPhone");
        result.Value.RegistrationApplicationSubmitted.Should().BeTrue();
        result.Value.ApplicationStatus.ToString().Should().Be("ApprovedByRegulator");
        result.Value.RegistrationReferenceNumber.Should().Be("TestRef");
    }

    [TestMethod]
    public async Task LatestBrandValidationEvent_IsNull_Handle_ShouldReturnApprovedByRegulator_WhenSubmitted()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = fileId
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var latestBrandAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Brands,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-4),
            FileId = fileId
        };

        var latestBrandAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-4)
        };

        BrandValidationEvent latestBrandValidationEvent = null;

        var latestPartnershipAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Partnerships,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-3),
            FileId = fileId
        };

        var latestPartnershipAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-3)
        };

        var latestPartnershipValidationEvent = new PartnerValidationEvent
        {
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-3),
            IsValid = true,
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now,
            FileId = fileId,
            SubmittedBy = "User1"
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(2),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(2)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(3),
            Decision = RegulatorDecision.Approved,
            DecisionDate = DateTime.Now.AddMinutes(3),
            RegistrationReferenceNumber = "TestRef"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestPartnershipAntivirusResultEvent,
                latestBrandAntivirusCheckEvent,
                latestBrandAntivirusResultEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                latestPartnershipAntivirusCheckEvent,
                latestBrandValidationEvent,
                latestPartnershipValidationEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile.FileId.Should().Be(submissionEvent.FileId);
        result.Value.LastSubmittedFile.SubmittedByName.Should().Be(submissionEvent.SubmittedBy);
        result.Value.LastSubmittedFile.SubmittedDateTime.Should().Be(submissionEvent.Created);
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.RegistrationFeePaymentMethod!.Should().Be("PayByPhone");
        result.Value.RegistrationApplicationSubmitted.Should().BeTrue();
        result.Value.ApplicationStatus.ToString().Should().Be("ApprovedByRegulator");
        result.Value.RegistrationReferenceNumber.Should().Be("TestRef");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Handle_Should_Set_latestCompanyDetailsCreatedDatetime_When_validation(bool validationPass)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = false,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = validationPass,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                registrationValidationEvent,
                latestCompanyDetailsAntivirusResultEvent
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.Should().Be(GetRegistrationApplicationDetailsResponse.ApplicationStatusType.NotStarted);
        result.Value.Should().BeEquivalentTo(new GetRegistrationApplicationDetailsResponse
        {
            SubmissionId = submissionId,
            IsSubmitted = false,
            IsResubmission = null,
            ApplicationReferenceNumber = applicationReferenceNumber,
            LastSubmittedFile = new GetRegistrationApplicationDetailsResponse.LastSubmittedFileDetails { SubmittedByName = null },
            RegistrationApplicationSubmittedDate = null,
            RegistrationApplicationSubmittedComment = null,
            RegistrationReferenceNumber = null,
            ApplicationStatus = GetRegistrationApplicationDetailsResponse.ApplicationStatusType.NotStarted
        });
    }

    [TestMethod]
    public async Task Handle_Should_Ignore_RegulatorDecisionEvent_When_Decision_Is_Invalid()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now,
            FileId = fileId,
            SubmittedBy = "User1"
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(2),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(2)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(3),
            Decision = RegulatorDecision.None,
            DecisionDate = DateTime.Now.AddMinutes(3),
            RegistrationReferenceNumber = "TestRef"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestCompanyDetailsAntivirusResultEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent
            }.BuildMock());

        // Act
        var action = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        action.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled, false)]
    [DataRow(RegulatorDecision.Rejected, false)]
    [DataRow(RegulatorDecision.Approved, true)]
    [DataRow(RegulatorDecision.Queried, true)]
    [DataRow(RegulatorDecision.Accepted, true)]
    public async Task Handle_ShouldSetHasAnyApprovedOrQueriedRegulatorDecision_Correctly_AsPer_LatestDecision(RegulatorDecision decision, bool expectedHasAnyApprovedOrQueriedRegulatorDecision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "Jan to Jun 2026",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(-5)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            Decision = decision,
            DecisionDate = DateTime.Now.AddMinutes(-5),
            RegistrationReferenceNumber = "TestRef"
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            FileId = fileId,
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent,
                submissionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.HasAnyApprovedOrQueriedRegulatorDecision.Should().Be(expectedHasAnyApprovedOrQueriedRegulatorDecision);
    }

    [TestMethod]
    [DataRow(RegulatorDecision.Queried, false)]
    public async Task Handle_ShouldSetHasAnyApprovedOrQueriedRegulatorDecision_Correctly_AsPer_LatestDecision2025(RegulatorDecision decision, bool expectedHasAnyApprovedOrQueriedRegulatorDecision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "Jan to Jun 2025",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var feePaymentEvent = new RegistrationFeePaymentEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            PaymentMethod = "PayByPhone"
        };

        var applicationSubmittedEvent = new RegistrationApplicationSubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            ApplicationReferenceNumber = applicationReferenceNumber,
            SubmissionDate = DateTime.Now.AddMinutes(-5)
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            Decision = decision,
            DecisionDate = DateTime.Now.AddMinutes(-5),
            RegistrationReferenceNumber = "TestRef"
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
        };

        var registrationValidationEvent = new RegistrationValidationEvent
        {
            ErrorCount = 0,
            WarningCount = 0,
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var latestCompanyDetailsAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.CompanyDetails,
            SubmissionId = submissionId,
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var submissionEvent2 = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(1),
            FileId = fileId,
            SubmittedBy = "User1"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[]
            {
                submissionEvent,
                registrationValidationEvent,
                latestCompanyDetailsAntivirusCheckEvent,
                latestCompanyDetailsAntivirusResultEvent,
                registrationDecisionEvent,
                feePaymentEvent,
                applicationSubmittedEvent,
                submissionEvent2
            }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.HasAnyApprovedOrQueriedRegulatorDecision.Should().Be(expectedHasAnyApprovedOrQueriedRegulatorDecision);
    }
}