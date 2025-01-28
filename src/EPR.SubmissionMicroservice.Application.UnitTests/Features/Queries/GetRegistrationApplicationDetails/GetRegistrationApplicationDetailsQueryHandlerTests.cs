using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
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

        var latestBrandAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Brands,
            SubmissionId = submissionId,
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestBrandAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var latestBrandValidationEvent = new BrandValidationEvent
        {
            SubmissionId = submissionId,
            Created = DateTime.Now,
            IsValid = true,
        };

        var latestPartnershipAntivirusCheckEvent = new AntivirusCheckEvent
        {
            FileType = FileType.Partnerships,
            SubmissionId = submissionId,
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestPartnershipAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now,
        };

        var latestPartnershipValidationEvent = new PartnerValidationEvent
        {
            SubmissionId = submissionId,
            Created = DateTime.Now,
            IsValid = true,
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
            Decision = RegulatorDecision.Approved,
            DecisionDate = DateTime.Now.AddMinutes(-5)
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
    }

    [TestMethod]
    public async Task Handle_ShouldReturnCorrectResponse_WhenPaymentAndApplicationSubmittedEventsFound()
    {
        // Arrange
        var applicationReferenceNumber = "TestRef";

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

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
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

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submissionEvent, feePaymentEvent, applicationSubmittedEvent }.BuildMock());

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
    [DataRow(RegulatorDecision.None)]
    public async Task Handle_ShouldSetStatusToRegulator_WhenRegulatorDecisionExists(RegulatorDecision decision)
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
            Created = DateTime.Now,
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var submissionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            Decision = decision,
            DecisionDate = DateTime.Now.AddMinutes(-5)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submissionEvent, latestCompanyDetailsAntivirusCheckEvent, latestCompanyDetailsAntivirusResultEvent, registrationValidationEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        if (decision == RegulatorDecision.None)
        {
            result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
        }
        else
        {
            result.Value.ApplicationStatus.ToString().Should().Be($"{decision.ToString()}ByRegulator");
        }
    }

    [TestMethod]
    public async Task Handle_ShouldSetStatusToRegulator_WhenRegulatorDecisionInvalid_thenSubmittedAndHasRecentFileUpload()
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
            IsSubmitted = true
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
            RequiresBrandsFile = false,
            RequiresPartnershipsFile = false,
            IsValid = true,
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

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            Decision = RegulatorDecision.None,
            DecisionDate = DateTime.Now.AddMinutes(-5)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submissionEvent, registrationDecisionEvent, latestCompanyDetailsAntivirusCheckEvent, latestCompanyDetailsAntivirusResultEvent, registrationValidationEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedAndHasRecentFileUpload");
    }

    [TestMethod]
    public async Task Handle_ShouldSetStatusToRegulator_WhenRegulatorDecisionInvalid_thenSubmittedToRegulator()
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
            IsSubmitted = true
        };

        var submissionEvent = new SubmittedEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now,
            FileId = Guid.NewGuid(),
            SubmittedBy = "User1"
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
            Created = DateTime.Now.AddMinutes(5),
            FileId = fileId,
        };

        var latestCompanyDetailsAntivirusResultEvent = new AntivirusResultEvent
        {
            FileId = fileId,
            SubmissionId = submissionId,
            Created = DateTime.Now
        };

        var registrationDecisionEvent = new RegulatorRegistrationDecisionEvent
        {
            SubmissionId = submission.Id,
            Created = DateTime.Now.AddMinutes(-5),
            Decision = RegulatorDecision.None,
            DecisionDate = DateTime.Now.AddMinutes(-5)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submissionEvent, registrationDecisionEvent, latestCompanyDetailsAntivirusCheckEvent, latestCompanyDetailsAntivirusResultEvent, registrationValidationEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedAndHasRecentFileUpload");
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