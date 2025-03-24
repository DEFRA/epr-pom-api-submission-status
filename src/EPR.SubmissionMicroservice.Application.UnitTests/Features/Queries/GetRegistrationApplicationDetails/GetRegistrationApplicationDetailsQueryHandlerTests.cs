﻿using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;
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
    private readonly Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventQueryRepositoryMock;
    private readonly GetRegistrationApplicationDetailsQueryHandler _handler;

    public GetRegistrationApplicationDetailsQueryHandlerTests()
    {
        _submissionQueryRepositoryMock = new Mock<IQueryRepository<Submission>>();
        _submissionEventQueryRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _handler = new GetRegistrationApplicationDetailsQueryHandler(
            _submissionQueryRepositoryMock.Object,
            _submissionEventQueryRepositoryMock.Object);
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
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
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
    [DataRow(true, 0, "2025/01/29")]
    [DataRow(true, 1, "2025/01/29")]
    [DataRow(false, 1, "2025/01/31")]
    [DataRow(true, 2, "2025/01/29")]
    [DataRow(false, 2, "2025/01/31")]
    public async Task Handle_ShouldReturnIsLateFeeApplicable_WhenApplicationSubmittedEventsFound(bool isLateFeeApplicable, int numberOfSubmissions, string? lateSubmissionDeadline)
    {
        // Arrange
        var applicationReferenceNumber = "TestRef";
        var lateFeeDeadline = DateTime.Parse(lateSubmissionDeadline);
        var fileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();

        var query = new GetRegistrationApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "2024-Q1",
            LateFeeDeadline = lateFeeDeadline
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
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
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

        var applicationSubmittedEvent = new List<RegistrationApplicationSubmittedEvent>();

        for (var i = 0; i < numberOfSubmissions; i++)
        {
            applicationSubmittedEvent.Add(new RegistrationApplicationSubmittedEvent
            {
                SubmissionId = submission.Id,
                Created = DateTime.Now.AddMinutes(2),
                ApplicationReferenceNumber = applicationReferenceNumber,
                SubmissionDate = isLateFeeApplicable ? lateFeeDeadline.AddMinutes(15) : lateFeeDeadline.AddMinutes(-15)
            });
        }

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        var events = new List<AbstractSubmissionEvent> { latestCompanyDetailsAntivirusCheckEvent, latestCompanyDetailsAntivirusResultEvent, registrationValidationEvent, submissionEvent, feePaymentEvent };
        events.AddRange(applicationSubmittedEvent);

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(events.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.LastSubmittedFile.Should().NotBeNull();
        result.Value.LastSubmittedFile!.SubmittedByName.Should().Be("User1");
        result.Value.IsLateFeeApplicable.Should().Be(isLateFeeApplicable);
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
    public async Task Handle_ShouldNotSetLastSubmittedFileDetails_WhenLatestFileIsNotSubmitted(RegulatorDecision decision)
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
        result.Value.LastSubmittedFile.Should().BeNull();
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.RegistrationFeePaymentMethod!.Should().BeNull();
        result.Value.RegistrationApplicationSubmitted.Should().BeFalse();
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedAndHasRecentFileUpload");
        result.Value.RegistrationReferenceNumber.Should().Be("TestRef");
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
        result.Value.ApplicationReferenceNumber!.Should().Be(applicationReferenceNumber);
        result.Value.RegistrationFeePaymentMethod!.Should().BeNull();
        result.Value.RegistrationApplicationSubmitted.Should().BeFalse();
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
        result.Value.RegistrationReferenceNumber.Should().Be("TestRef");
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
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
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
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
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
    public async Task Handle_ShouldSetLateFeeToFalse_When_ReSubmission_Is_True_And_LateFeeDeadline_Already_Pass()
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
            ComplianceSchemeId = complianceSchemeId,
            LateFeeDeadline = previousCreated
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
            Created = previousCreated.AddMinutes(-10),
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
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
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
            SubmittedBy = "User1",
            IsResubmission = true
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
        result.Value.IsLateFeeApplicable.Should().BeFalse();
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
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
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
            RequiresBrandsFile = true,
            RequiresPartnershipsFile = true,
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
        result.Value.RegistrationReferenceNumber.Should().Be("TestRef");
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
}