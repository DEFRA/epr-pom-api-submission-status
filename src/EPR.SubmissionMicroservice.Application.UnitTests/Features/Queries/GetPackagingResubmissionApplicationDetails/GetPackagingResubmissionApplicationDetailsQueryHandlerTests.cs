using EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;
using static EPR.SubmissionMicroservice.Application.Features.Queries.Common.GetPackagingResubmissionApplicationDetailsResponse;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.GetPackagingResubmissionApplicationDetails;

[TestClass]
public class GetPackagingResubmissionApplicationDetailsQueryHandlerTests
{
    private readonly Mock<IQueryRepository<Submission>> _submissionQueryRepositoryMock;
    private readonly Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventQueryRepositoryMock;
    private readonly Mock<IQueryRepository<AbstractValidationError>> _validationErrorQueryRepositoryMock;
    private readonly Mock<IQueryRepository<AbstractValidationWarning>> _validationWarningRepositoryMock;
    private readonly GetPackagingResubmissionApplicationDetailsQueryHandler _handler;

    public GetPackagingResubmissionApplicationDetailsQueryHandlerTests()
    {
        _submissionQueryRepositoryMock = new Mock<IQueryRepository<Submission>>();
        _submissionEventQueryRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _submissionEventQueryRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _validationErrorQueryRepositoryMock = new Mock<IQueryRepository<AbstractValidationError>>();
        _validationWarningRepositoryMock = new Mock<IQueryRepository<AbstractValidationWarning>>();

        _handler = new GetPackagingResubmissionApplicationDetailsQueryHandler(
            _submissionQueryRepositoryMock.Object,
            _submissionEventQueryRepositoryMock.Object,
            _validationErrorQueryRepositoryMock.Object,
            _validationWarningRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_ShouldReturnNull_WhenNoSubmissionIsFound()
    {
        // Arrange
        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(Enumerable.Empty<Submission>().BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task Handle_ShouldReturnNullFields_WhenNoEventsAssociated()
    {
        // Arrange
        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            AppReferenceNumber = "test"
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(Enumerable.Empty<AbstractSubmissionEvent>().BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Count.Should().Be(1);
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().IsSubmitted.Should().BeFalse();
        result.Value.First().ApplicationReferenceNumber.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handle_ShouldReturnSubmission_WhenReferenceNumberEventIsAfterAntivirusCheckEvent()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var events = new List<AbstractSubmissionEvent>
         {
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                Created = DateTime.Now,
                FileId = fileId,
            },
            new AntivirusResultEvent
            {
                BlobName = "test",
                SubmissionId = submissionId,
                FileId = fileId
            },
            new CheckSplitterValidationEvent
            {
                BlobName = "test",
                SubmissionId = submissionId,
                DataCount = 1,
            },
            new ProducerValidationEvent
            {
                BlobName = "test",
                SubmissionId = submissionId,
                IsValid = true,
                ErrorCount = 2,
                WarningCount = 2,
                Created = DateTime.Now.AddMinutes(-5)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
            },
            new RegulatorPoMDecisionEvent
            {
                SubmissionId = submissionId,
                Decision = RegulatorDecision.Rejected,
                IsResubmissionRequired = true
            },
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "test",
                Created = DateTime.Now
            }
         };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
                .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
                .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        _validationErrorQueryRepositoryMock
                .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
                .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
                .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
                .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
    }

    [TestMethod]
    public async Task Handle_ShouldReturnFileUploadedStatus_WhenSubmissionHasNotHappenedYet()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                Created = DateTime.Now,
                FileId = fileId,
            },
            new AntivirusResultEvent
            {
                BlobName = "test",
                SubmissionId = submissionId,
                FileId = fileId
            },
            new CheckSplitterValidationEvent
            {
                BlobName = "test",
                SubmissionId = submissionId,
                DataCount = 1,
            },
            new ProducerValidationEvent
            {
                BlobName = "test",
                SubmissionId = submissionId,
                IsValid = true,
                Created = DateTime.Now.AddMinutes(-5)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
                Created = DateTime.Now.AddMinutes(-40)
            },
            new RegulatorPoMDecisionEvent
            {
                SubmissionId = submissionId,
                Decision = RegulatorDecision.Rejected,
                IsResubmissionRequired = true
            },
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "test",
                Created = DateTime.Now.AddMinutes(-40)
            }
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
                .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
                .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        _validationErrorQueryRepositoryMock
                .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
                .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
                .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
                .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.ToString().Should().Be("FileUploaded");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnSubmittedAndHasRecentFileUpload_WhenSubmittedButHasNewFileUploaded()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var applicationReferenceNumber = "TestRef";

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = applicationReferenceNumber
        };

        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                Created = DateTime.Now.AddMinutes(-5),
                FileId = fileId,
            },
            new AntivirusCheckEvent
            {
                FileType = FileType.Pom,
                SubmissionId = submissionId,
                Created = DateTime.Now.AddMinutes(-5),
                FileId = fileId,
            },
            new AntivirusResultEvent
            {
                BlobName = "test",
                SubmissionId = submissionId,
                FileId = fileId
            },
            new AntivirusResultEvent
            {
                SubmissionId = submissionId,
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = submissionId,
                DataCount = 2,
                Created = DateTime.Now
            },
            new CheckSplitterValidationEvent
            {
                BlobName = "test",
                SubmissionId = submissionId,
                DataCount = 1,
            },
            new ProducerValidationEvent
            {
                BlobName = "test",
                SubmissionId = submissionId,
                IsValid = true,
                Created = DateTime.Now.AddMinutes(-5)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
            },
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "test",
                Created = DateTime.Now.AddMinutes(-40)
            }
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        _validationErrorQueryRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
            .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.ToString().Should().Be("FileUploaded");
    }

    // SUB-332: the latest upload has not validated, so the upload step stays NotStarted and startable. The
    // added reference-number assertion covers the cycle's identity surviving that status.
    [TestMethod]
    public async Task Handle_ShouldReturnNotStartedAndKeepTheReferenceNumber_WhenRegulatorPackagingDecisionEventisAcceptedAndUploadIsIncomplete()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef"
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Accepted,
            IsResubmissionRequired = true
        };

        var antivirusCheck = new AntivirusCheckEvent
        {
            SubmissionId = submissionId,
            FileType = FileType.Pom,
            Created = DateTime.Now
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { regulatorPoMDecisionEvent, antivirusCheck, packagingResubmissionReferenceNumberCreatedEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.Should().Be(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationReferenceNumber.Should().Be("Test");
    }

    // SUB-332: see the accepted-decision test above.
    [TestMethod]
    public async Task Handle_ShouldReturnNotStartedAndKeepTheReferenceNumber_WhenRegulatorPackagingDecisionEventisApprovedAndUploadIsIncomplete()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef"
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Approved,
            IsResubmissionRequired = true
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            Created = DateTime.Now,
            FileType = FileType.Pom
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { regulatorPoMDecisionEvent, packagingResubmissionReferenceNumberCreatedEvent, antivirusCheckEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.Should().Be(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationReferenceNumber.Should().Be("Test");
    }

    // SUB-332: this is the incident's starting state - the regulator rejected the submission and the user
    // has entered the resubmission journey, with nothing valid uploaded into it yet.
    [TestMethod]
    public async Task Handle_ShouldReturnNotStartedAndKeepTheReferenceNumber_WhenRegulatorPackagingDecision_EventisRejectedByRegulator()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef"
        };

        var antivirusCheck = new AntivirusCheckEvent
        {
            SubmissionId = submissionId,
            FileType = FileType.Pom,
            Created = DateTime.Now
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Rejected,
            IsResubmissionRequired = true
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { antivirusCheck, regulatorPoMDecisionEvent, packagingResubmissionReferenceNumberCreatedEvent }.BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.Should().Be(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationReferenceNumber.Should().Be("Test");
    }

    [TestMethod]
    public async Task Handle_ShoulSetResubmissionFeePaymentMethodToNull_WhenPackagingFeePaymentEventCreated_ISbeforeregulatorPackagingDecisionEvent()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef"
        };

        var submittedEvent = new SubmittedEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now.AddMinutes(10)
        };

        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            FileType = FileType.Pom,
            Created = DateTime.Now
        };

        var antivirusResultEvent = new AntivirusResultEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Queried,
            IsResubmissionRequired = true,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var checkSplitterValidationEvent = new CheckSplitterValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true,
            DataCount = 1
        };

        var producerValidationEvent = new ProducerValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true
        };

        var packagingApplicationSubmittedEvents = new PackagingResubmissionApplicationSubmittedCreatedEvent
        {
            Created = DateTime.Now.AddMinutes(-20),
            IsResubmitted = true
        };

        var packagingDataResubmissionFeePaymentEvent = new PackagingDataResubmissionFeePaymentEvent
        {
            SubmissionId = submissionId,
            ReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submittedEvent, antivirusCheckEvent, antivirusResultEvent, checkSplitterValidationEvent, producerValidationEvent, regulatorPoMDecisionEvent, packagingDataResubmissionFeePaymentEvent, packagingApplicationSubmittedEvents, packagingResubmissionReferenceNumberCreatedEvent }.BuildMock());

        _validationErrorQueryRepositoryMock
           .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
           .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ResubmissionFeePaymentMethod.Should().Be(null);
        result.Value.First().ResubmissionApplicationSubmittedComment.Should().BeNull();
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().BeNull();
        result.Value.First().ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnSubmittedAndHasRecentFileUpload_WhenRegulatorPackagingDecisionEventisCancelledAndIfSubmissionIsDoneBeforeFileUpload()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef"
        };

        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            FileType = FileType.Pom,
            Created = DateTime.Now
        };

        var antivirusResultEvent = new AntivirusResultEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Cancelled,
            IsResubmissionRequired = true,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var checkSplitterValidationEvent = new CheckSplitterValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true,
            DataCount = 1
        };

        var producerValidationEvent = new ProducerValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { antivirusCheckEvent, antivirusResultEvent, checkSplitterValidationEvent, producerValidationEvent, regulatorPoMDecisionEvent, packagingResubmissionReferenceNumberCreatedEvent }.BuildMock());

        _validationErrorQueryRepositoryMock
           .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
           .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnSubmittedToRegulator_WhenRegulatorPackagingDecisionEventisCancelledAndIfSubmissionIsDoneAfterFileUpload()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef"
        };

        var submittedEvent = new SubmittedEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now.AddMinutes(10)
        };

        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            FileType = FileType.Pom,
            Created = DateTime.Now
        };

        var antivirusResultEvent = new AntivirusResultEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Cancelled,
            IsResubmissionRequired = true,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var checkSplitterValidationEvent = new CheckSplitterValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true,
            DataCount = 1
        };

        var producerValidationEvent = new ProducerValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submittedEvent, antivirusCheckEvent, antivirusResultEvent, checkSplitterValidationEvent, producerValidationEvent, regulatorPoMDecisionEvent, packagingResubmissionReferenceNumberCreatedEvent }.BuildMock());

        _validationErrorQueryRepositoryMock
           .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
           .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnSubmittedAndHasRecentFileUpload_WhenRegulatorPackagingDecisionEventisQueriedAndIfSubmissionIsDoneBeforeFileUpload()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef"
        };

        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            FileType = FileType.Pom,
            Created = DateTime.Now
        };

        var antivirusResultEvent = new AntivirusResultEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Queried,
            IsResubmissionRequired = true,
            Created = DateTime.Now.AddMinutes(1)
        };

        var checkSplitterValidationEvent = new CheckSplitterValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true,
            DataCount = 1
        };

        var producerValidationEvent = new ProducerValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { antivirusCheckEvent, antivirusResultEvent, checkSplitterValidationEvent, producerValidationEvent, regulatorPoMDecisionEvent, packagingResubmissionReferenceNumberCreatedEvent }.BuildMock());

        _validationErrorQueryRepositoryMock
           .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
           .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnSubmittedToRegulator_WhenRegulatorPackagingDecisionEventisQueriedAndIfSubmissionIsDoneAfterFileUpload()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef"
        };

        var submittedEvent = new SubmittedEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now.AddMinutes(10)
        };

        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            FileType = FileType.Pom,
            Created = DateTime.Now
        };

        var antivirusResultEvent = new AntivirusResultEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Queried,
            IsResubmissionRequired = true,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var checkSplitterValidationEvent = new CheckSplitterValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true,
            DataCount = 1
        };

        var producerValidationEvent = new ProducerValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submittedEvent, antivirusCheckEvent, antivirusResultEvent, checkSplitterValidationEvent, producerValidationEvent, regulatorPoMDecisionEvent, packagingResubmissionReferenceNumberCreatedEvent }.BuildMock());

        _validationErrorQueryRepositoryMock
           .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
           .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnPayByPhonePaymentMethod_WhenSubmissionIsPaidUsingPayByPhonePaymentMethod()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef",
        };

        var submittedEvent = new SubmittedEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now.AddMinutes(-10)
        };

        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            FileType = FileType.Pom,
            Created = DateTime.Now
        };

        var antivirusResultEvent = new AntivirusResultEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Queried,
            IsResubmissionRequired = true,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var checkSplitterValidationEvent = new CheckSplitterValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true,
            DataCount = 1
        };

        var producerValidationEvent = new ProducerValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true
        };

        var packagingDataResubmissionFeePaymentEvent = new PackagingDataResubmissionFeePaymentEvent
        {
            SubmissionId = submissionId,
            PaymentMethod = "PayByPhone",
            ReferenceNumber = "Test",
            Created = DateTime.Now
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submittedEvent, antivirusCheckEvent, antivirusResultEvent, checkSplitterValidationEvent, producerValidationEvent, packagingDataResubmissionFeePaymentEvent, regulatorPoMDecisionEvent, packagingResubmissionReferenceNumberCreatedEvent }.BuildMock());

        _validationErrorQueryRepositoryMock
           .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
           .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.ToString().Should().Be("FileUploaded");
        result.Value.First().ResubmissionFeePaymentMethod!.Should().Be("PayByPhone");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnApplicationStatusOfNotStarted_WhenCheckSplitterContainsErrorsAndResubmissionApplicationSubmitted()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef",
        };

        var submittedEvent = new SubmittedEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now.AddMinutes(-10)
        };

        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            FileType = FileType.Pom,
            Created = DateTime.Now
        };

        var antivirusResultEvent = new AntivirusResultEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Queried,
            IsResubmissionRequired = true,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var checkSplitterValidationEvent = new CheckSplitterValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true,
            DataCount = 1,
            Errors = new List<string>() { "new error" },
            ErrorCount = 1,
        };

        var producerValidationEvent = new ProducerValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true
        };

        var packagingDataResubmissionFeePaymentEvent = new PackagingDataResubmissionFeePaymentEvent
        {
            SubmissionId = submissionId,
            PaymentMethod = "PayByPhone",
            ReferenceNumber = "Test",
            Created = DateTime.Now
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        var packagingResubmissionApplicationSubmittedCreatedEvent = new PackagingResubmissionApplicationSubmittedCreatedEvent
        {
            IsResubmitted = true,
            SubmissionDate = DateTime.Now.AddDays(-99),
            Created = DateTime.Now
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submittedEvent, antivirusCheckEvent, antivirusResultEvent, checkSplitterValidationEvent, producerValidationEvent, packagingDataResubmissionFeePaymentEvent, regulatorPoMDecisionEvent, packagingResubmissionReferenceNumberCreatedEvent, packagingResubmissionApplicationSubmittedCreatedEvent }.BuildMock());

        _validationErrorQueryRepositoryMock
           .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
           .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.ToString().Should().Be("NotStarted");

        // SUB-332: NotStarted reports that no cycle is open, it no longer blanks the cycle's own state, so
        // the fee paid after the last submit is still reported alongside the reference number.
        result.Value.First().ApplicationReferenceNumber.Should().Be("Test");
        result.Value.First().ResubmissionFeePaymentMethod.Should().Be("PayByPhone");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnApplicationStatusOfNotStarted_WhenCheckSplitterContainsErrors()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef",
        };

        var submittedEvent = new SubmittedEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now.AddMinutes(-10)
        };

        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            FileType = FileType.Pom,
            Created = DateTime.Now
        };

        var antivirusResultEvent = new AntivirusResultEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Queried,
            IsResubmissionRequired = true,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var checkSplitterValidationEvent = new CheckSplitterValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true,
            DataCount = 1,
            Errors = new List<string>() { "new error" },
            ErrorCount = 1,
        };

        var producerValidationEvent = new ProducerValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true
        };

        var packagingDataResubmissionFeePaymentEvent = new PackagingDataResubmissionFeePaymentEvent
        {
            SubmissionId = submissionId,
            PaymentMethod = "PayByPhone",
            ReferenceNumber = "Test",
            Created = DateTime.Now
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        var packagingResubmissionApplicationSubmittedCreatedEvent = new PackagingResubmissionApplicationSubmittedCreatedEvent
        {
            IsResubmitted = true,
            SubmissionDate = DateTime.Now.AddDays(-99),
            Created = DateTime.Now
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submittedEvent, antivirusCheckEvent, antivirusResultEvent, checkSplitterValidationEvent, producerValidationEvent, packagingDataResubmissionFeePaymentEvent, regulatorPoMDecisionEvent, packagingResubmissionReferenceNumberCreatedEvent, packagingResubmissionApplicationSubmittedCreatedEvent }.BuildMock());

        _validationErrorQueryRepositoryMock
           .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
           .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.ToString().Should().Be("NotStarted");
    }

    [TestMethod]
    public async Task Handle_ShouldIgnoreOfflinePayments_ReturnsLatestPayByPhoneOrPaybyBankOrPayOnlinePaymentMethod_WhenSubmissionHasOfflinePaymentMethodAsLatest()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef",
        };

        var submittedEvent = new SubmittedEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now.AddMinutes(-10)
        };

        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            FileType = FileType.Pom,
            Created = DateTime.Now
        };

        var antivirusResultEvent = new AntivirusResultEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Queried,
            IsResubmissionRequired = true,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var checkSplitterValidationEvent = new CheckSplitterValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true,
            DataCount = 1
        };

        var producerValidationEvent = new ProducerValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true
        };

        var packagingDataResubmissionFeePaymentEvent = new PackagingDataResubmissionFeePaymentEvent
        {
            SubmissionId = submissionId,
            PaymentMethod = "PayByPhone",
            ReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-5)
        };

        var packagingDataResubmissionFeePaymentEvent2 = new PackagingDataResubmissionFeePaymentEvent
        {
            SubmissionId = submissionId,
            PaymentMethod = "Offline",
            ReferenceNumber = "Test",
            Created = DateTime.Now
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submittedEvent, antivirusCheckEvent, antivirusResultEvent, checkSplitterValidationEvent, producerValidationEvent, packagingDataResubmissionFeePaymentEvent, packagingDataResubmissionFeePaymentEvent2, regulatorPoMDecisionEvent, packagingResubmissionReferenceNumberCreatedEvent }.BuildMock());

        _validationErrorQueryRepositoryMock
           .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
           .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.ToString().Should().Be("FileUploaded");
        result.Value.First().ResubmissionFeePaymentMethod!.Should().Be("PayByPhone");
        result.Value.First().ResubmissionFeePaymentMethod!.Should().NotBe("Offline");
    }

    [TestMethod]
    public async Task Handle_ShouldIgnoreOfflinePayments_ReturnsLatestPayByPhoneOrPaybyBankOrPayOnlinePaymentMethod_WhenSubmissionDoesNotHaveOfflinePaymentMethodAsLatest()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef",
        };

        var submittedEvent = new SubmittedEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now.AddMinutes(-10)
        };

        var antivirusCheckEvent = new AntivirusCheckEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            FileType = FileType.Pom,
            Created = DateTime.Now
        };

        var antivirusResultEvent = new AntivirusResultEvent
        {
            SubmissionId = submissionId,
            FileId = fileId,
            Created = DateTime.Now
        };

        var regulatorPoMDecisionEvent = new RegulatorPoMDecisionEvent
        {
            Decision = RegulatorDecision.Queried,
            IsResubmissionRequired = true,
            Created = DateTime.Now.AddMinutes(-5)
        };

        var checkSplitterValidationEvent = new CheckSplitterValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true,
            DataCount = 1
        };

        var producerValidationEvent = new ProducerValidationEvent
        {
            Created = DateTime.Now.AddMinutes(-5),
            IsValid = true
        };

        var packagingDataResubmissionFeePaymentEvent = new PackagingDataResubmissionFeePaymentEvent
        {
            SubmissionId = submissionId,
            PaymentMethod = "Offline",
            ReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-5)
        };

        var packagingDataResubmissionFeePaymentEvent2 = new PackagingDataResubmissionFeePaymentEvent
        {
            SubmissionId = submissionId,
            PaymentMethod = "PayByPhone",
            ReferenceNumber = "Test",
            Created = DateTime.Now
        };

        var packagingResubmissionReferenceNumberCreatedEvent = new PackagingResubmissionReferenceNumberCreatedEvent
        {
            SubmissionId = submissionId,
            PackagingResubmissionReferenceNumber = "Test",
            Created = DateTime.Now.AddMinutes(-20)
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns(new AbstractSubmissionEvent[] { submittedEvent, antivirusCheckEvent, antivirusResultEvent, checkSplitterValidationEvent, producerValidationEvent, packagingDataResubmissionFeePaymentEvent, packagingDataResubmissionFeePaymentEvent2, regulatorPoMDecisionEvent, packagingResubmissionReferenceNumberCreatedEvent }.BuildMock());

        _validationErrorQueryRepositoryMock
           .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
           .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().SubmissionId.Should().Be(submission.Id);
        result.Value.First().ApplicationStatus.ToString().Should().Be("FileUploaded");
        result.Value.First().ResubmissionFeePaymentMethod!.Should().Be("PayByPhone");
        result.Value.First().ResubmissionFeePaymentMethod!.Should().NotBe("Offline");
    }

    // SUB-332: an upload that never produced a valid file leaves the upload step startable, so the user can
    // replace it, while the cycle's reference number is still reported so the journey stays reachable.
    [TestMethod]
    public async Task Handle_ShouldReturnNotStartedAndKeepTheReferenceNumber_WhenLaterUploadFailsValidationAfterAValidFileWasSubmitted()
    {
        // Arrange - the SUB-332 incident: regulator rejects, reference number created, file A
        // validates and is submitted, then file B is uploaded but its validation never completes.
        var submissionId = Guid.NewGuid();
        var fileA = Guid.NewGuid();
        var fileB = Guid.NewGuid();
        var now = DateTime.Now;

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            new RegulatorPoMDecisionEvent
            {
                SubmissionId = submissionId,
                Decision = RegulatorDecision.Rejected,
                IsResubmissionRequired = true,
                Created = now.AddMinutes(-60)
            },
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "PEPR12345S01",
                Created = now.AddMinutes(-50)
            },
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = fileA,
                Created = now.AddMinutes(-40)
            },
            new AntivirusResultEvent
            {
                SubmissionId = submissionId,
                FileId = fileA,
                BlobName = "blob-a",
                Created = now.AddMinutes(-39)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-a",
                DataCount = 1,
                IsValid = true,
                Created = now.AddMinutes(-38)
            },
            new ProducerValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-a",
                IsValid = true,
                Created = now.AddMinutes(-37)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
                FileId = fileA,
                SubmittedBy = "Test User",
                Created = now.AddMinutes(-30)
            },

            // File B: uploaded and scanned, but the Redis timeout meant the check splitter never
            // ran, so no CheckSplitterValidationEvent or ProducerValidationEvent exists for it.
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = fileB,
                Created = now.AddMinutes(-28)
            },
            new AntivirusResultEvent
            {
                SubmissionId = submissionId,
                FileId = fileB,
                BlobName = "blob-b",
                Created = now.AddMinutes(-27)
            }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.First().ApplicationStatus.Should().Be(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR12345S01");
        result.Value.First().LastSubmittedFile!.FileId.Should().Be(fileA);
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_ShouldReturnNotStartedAndKeepTheReferenceNumber_WhenNothingHasBeenUploadedSinceTheReferenceNumberWasCreated()
    {
        // Arrange - the user has entered the resubmission journey but not yet uploaded a file.
        var submissionId = Guid.NewGuid();
        var fileA = Guid.NewGuid();
        var now = DateTime.Now;

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = fileA,
                Created = now.AddMinutes(-60)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
                FileId = fileA,
                Created = now.AddMinutes(-55)
            },
            new RegulatorPoMDecisionEvent
            {
                SubmissionId = submissionId,
                Decision = RegulatorDecision.Rejected,
                IsResubmissionRequired = true,
                Created = now.AddMinutes(-50)
            },
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "PEPR12345S01",
                Created = now.AddMinutes(-40)
            }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.First().ApplicationStatus.Should().Be(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR12345S01");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnEarliestOpenReferenceNumber_WhenDuplicateReferenceNumberEventsExistInOneCycle()
    {
        // Arrange - the frontend created a second reference number mid-cycle because the first
        // response came back with an empty ApplicationReferenceNumber. The suffix is derived from
        // a submission-history count, so the two values are not identical.
        var submissionId = Guid.NewGuid();
        var fileA = Guid.NewGuid();
        var fileB = Guid.NewGuid();
        var now = DateTime.Now;

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "PEPR12345S01",
                Created = now.AddMinutes(-50)
            },
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = fileA,
                Created = now.AddMinutes(-40)
            },
            new AntivirusResultEvent
            {
                SubmissionId = submissionId,
                FileId = fileA,
                BlobName = "blob-a",
                Created = now.AddMinutes(-39)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-a",
                DataCount = 1,
                IsValid = true,
                Created = now.AddMinutes(-38)
            },
            new ProducerValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-a",
                IsValid = true,
                Created = now.AddMinutes(-37)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
                FileId = fileA,
                Created = now.AddMinutes(-30)
            },
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = fileB,
                Created = now.AddMinutes(-28)
            },

            // Duplicate raised after the empty ApplicationReferenceNumber was returned.
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "PEPR12345S02",
                Created = now.AddMinutes(-20)
            }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR12345S01");
    }

    [TestMethod]
    public async Task Handle_ShouldCloseCycle_WhenApplicationSubmittedEventFiresAfterTheReferenceNumber()
    {
        // Arrange - same shape as the open-cycle test, but the user reached the declaration step.
        var submissionId = Guid.NewGuid();
        var fileA = Guid.NewGuid();
        var now = DateTime.Now;
        var submissionDate = now.AddMinutes(-10);

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "PEPR12345S01",
                Created = now.AddMinutes(-50)
            },
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = fileA,
                Created = now.AddMinutes(-40)
            },
            new AntivirusResultEvent
            {
                SubmissionId = submissionId,
                FileId = fileA,
                BlobName = "blob-a",
                Created = now.AddMinutes(-39)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-a",
                DataCount = 1,
                IsValid = true,
                Created = now.AddMinutes(-38)
            },
            new ProducerValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-a",
                IsValid = true,
                Created = now.AddMinutes(-37)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
                FileId = fileA,
                Created = now.AddMinutes(-30)
            },
            new PackagingResubmissionApplicationSubmittedCreatedEvent
            {
                SubmissionId = submissionId,
                IsResubmitted = true,
                SubmissionDate = submissionDate,
                Comments = "Declared",
                Created = submissionDate
            }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.First().ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().Be(submissionDate);
        result.Value.First().ResubmissionApplicationSubmittedComment.Should().Be("Declared");
        result.Value.First().IsResubmitted.Should().BeTrue();
    }

    [TestMethod]
    public async Task Handle_ShouldOpenANewCycle_WhenAReferenceNumberIsCreatedAfterAPreviousApplicationSubmitted()
    {
        // Arrange - a completed cycle, then the regulator rejects again and a new cycle opens.
        // The closed cycle's declaration must not be reported against the new open one.
        var submissionId = Guid.NewGuid();
        var fileA = Guid.NewGuid();
        var fileB = Guid.NewGuid();
        var now = DateTime.Now;

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "PEPR12345S01",
                Created = now.AddMinutes(-90)
            },
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = fileA,
                Created = now.AddMinutes(-85)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
                FileId = fileA,
                Created = now.AddMinutes(-80)
            },
            new PackagingResubmissionApplicationSubmittedCreatedEvent
            {
                SubmissionId = submissionId,
                IsResubmitted = true,
                SubmissionDate = now.AddMinutes(-75),
                Comments = "First declaration",
                Created = now.AddMinutes(-75)
            },
            new RegulatorPoMDecisionEvent
            {
                SubmissionId = submissionId,
                Decision = RegulatorDecision.Rejected,
                IsResubmissionRequired = true,
                Created = now.AddMinutes(-60)
            },

            // Second cycle opens here.
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "PEPR12345S02",
                Created = now.AddMinutes(-50)
            },
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = fileB,
                Created = now.AddMinutes(-40)
            },
            new AntivirusResultEvent
            {
                SubmissionId = submissionId,
                FileId = fileB,
                BlobName = "blob-b",
                Created = now.AddMinutes(-39)
            }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR12345S02");
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().BeNull();
        result.Value.First().ResubmissionApplicationSubmittedComment.Should().BeNull();
        result.Value.First().IsResubmitted.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_ShouldReturnNotStartedAndKeepTheReferenceNumber_WhenLaterUploadFailsValidation_ForComplianceSchemePath()
    {
        // Arrange - the same incident against a compliance scheme submission.
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileA = Guid.NewGuid();
        var fileB = Guid.NewGuid();
        var now = DateTime.Now;

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" },
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId);

        var events = new List<AbstractSubmissionEvent>
        {
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "PEPR54321S01",
                Created = now.AddMinutes(-50)
            },
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = fileA,
                Created = now.AddMinutes(-40)
            },
            new AntivirusResultEvent
            {
                SubmissionId = submissionId,
                FileId = fileA,
                BlobName = "blob-a",
                Created = now.AddMinutes(-39)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-a",
                DataCount = 1,
                IsValid = true,
                Created = now.AddMinutes(-38)
            },
            new ProducerValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-a",
                IsValid = true,
                Created = now.AddMinutes(-37)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
                FileId = fileA,
                Created = now.AddMinutes(-30)
            },

            // Upload B produced a check splitter event promising 3 rows, but only 1 producer
            // validation event was written before validation stalled.
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = fileB,
                Created = now.AddMinutes(-28)
            },
            new AntivirusResultEvent
            {
                SubmissionId = submissionId,
                FileId = fileB,
                BlobName = "blob-b",
                Created = now.AddMinutes(-27)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-b",
                DataCount = 3,
                IsValid = true,
                Created = now.AddMinutes(-26)
            },
            new ProducerValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-b",
                IsValid = true,
                Created = now.AddMinutes(-25)
            }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.First().ApplicationStatus.Should().Be(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR54321S01");
    }

    // SUB-332: reporting "no open cycle" must not erase the cycle's identity. Replacing the response here
    // dropped ApplicationReferenceNumber, which drove the frontend to raise a second reference number for
    // a cycle that already existed.
    [TestMethod]
    public async Task Handle_ShouldPreserveReferenceNumberAndDeclaration_WhenClosedCycleHasAnInvalidLatestUpload()
    {
        // Arrange - cycle closed by a declaration, then a later upload whose validation never completed.
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var now = DateTime.Now;

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "PEPR11111S01",
                Created = now.AddMinutes(-50)
            },
            new PackagingResubmissionApplicationSubmittedCreatedEvent
            {
                SubmissionId = submissionId,
                IsResubmitted = true,
                SubmissionDate = now.AddMinutes(-40),
                Comments = "Declared",
                Created = now.AddMinutes(-40)
            },

            // A later upload with no check splitter event, so validation cannot pass.
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = fileId,
                Created = now.AddMinutes(-30)
            },
            new AntivirusResultEvent
            {
                SubmissionId = submissionId,
                FileId = fileId,
                BlobName = "blob-invalid",
                Created = now.AddMinutes(-29)
            }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().ApplicationStatus.Should().Be(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR11111S01");
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().NotBeNull();
    }

    // SUB-332: a declaration closes the cycle it belongs to, so submitting another file afterwards starts a
    // cycle the declaration says nothing about. Reporting it anyway marked the frontend's "submit to the
    // regulator" step complete before the new file had been declared, and sent its link to the confirmation
    // page instead of the declaration.
    [TestMethod]
    public async Task Handle_ShouldNotReportDeclaration_WhenAFileWasSubmittedAfterTheDeclaration()
    {
        // Arrange - declaration at -50, a further submit at -40, then a valid upload at -30.
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var now = DateTime.Now;

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "PEPR22222S01",
                Created = now.AddMinutes(-60)
            },
            new PackagingResubmissionApplicationSubmittedCreatedEvent
            {
                SubmissionId = submissionId,
                IsResubmitted = true,
                SubmissionDate = now.AddMinutes(-50),
                Comments = "Declared",
                Created = now.AddMinutes(-50)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
                FileId = fileId,
                Created = now.AddMinutes(-40)
            },
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = fileId,
                Created = now.AddMinutes(-30)
            },
            new AntivirusResultEvent
            {
                SubmissionId = submissionId,
                FileId = fileId,
                BlobName = "blob-valid",
                Created = now.AddMinutes(-29)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-valid",
                DataCount = 1,
                IsValid = true,
                Created = now.AddMinutes(-28)
            },
            new ProducerValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-valid",
                IsValid = true,
                Created = now.AddMinutes(-27)
            }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR22222S01");
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().BeNull();
        result.Value.First().ResubmissionApplicationSubmittedComment.Should().BeNull();
        result.Value.First().IsResubmitted.Should().BeNull();
    }

    // SUB-332: the reported flow end to end - accepted, resubmitted, rejected, then a reupload that was not
    // submitted, one that failed validation, and finally a valid file that was submitted and paid for. The
    // only reference number the frontend ever raises belongs to the first resubmission, so nothing but the
    // later submit distinguishes this cycle from that one.
    [TestMethod]
    public async Task Handle_ShouldReportTheFinalCycleAsAwaitingItsDeclaration_WhenAValidFileWasSubmittedAndPaidForAfterARejection()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var originalFile = Guid.NewGuid();
        var rejectedFile = Guid.NewGuid();
        var invalidFile = Guid.NewGuid();
        var finalFile = Guid.NewGuid();
        var now = DateTime.Now;

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            // The original submission, accepted by the regulator.
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = originalFile,
                Created = now.AddMinutes(-200)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
                FileId = originalFile,
                Created = now.AddMinutes(-190)
            },
            new RegulatorPoMDecisionEvent
            {
                SubmissionId = submissionId,
                Decision = RegulatorDecision.Accepted,
                Created = now.AddMinutes(-180)
            },

            // The first resubmission: the only reference number this submission ever gets.
            new PackagingResubmissionReferenceNumberCreatedEvent
            {
                SubmissionId = submissionId,
                PackagingResubmissionReferenceNumber = "PEPR33333S01",
                Created = now.AddMinutes(-170)
            },
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = rejectedFile,
                Created = now.AddMinutes(-160)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
                FileId = rejectedFile,
                Created = now.AddMinutes(-150)
            },
            new PackagingResubmissionFeeViewCreatedEvent
            {
                SubmissionId = submissionId,
                IsPackagingResubmissionFeeViewed = true,
                Created = now.AddMinutes(-145)
            },
            new PackagingDataResubmissionFeePaymentEvent
            {
                SubmissionId = submissionId,
                PaymentMethod = "PayByPhone",
                ReferenceNumber = "PEPR33333S01",
                Created = now.AddMinutes(-140)
            },
            new PackagingResubmissionApplicationSubmittedCreatedEvent
            {
                SubmissionId = submissionId,
                IsResubmitted = true,
                SubmissionDate = now.AddMinutes(-135),
                Comments = "First resubmission",
                Created = now.AddMinutes(-135)
            },
            new RegulatorPoMDecisionEvent
            {
                SubmissionId = submissionId,
                Decision = RegulatorDecision.Rejected,
                IsResubmissionRequired = true,
                Created = now.AddMinutes(-120)
            },

            // A reupload the user never submitted, then one that failed validation.
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = invalidFile,
                Created = now.AddMinutes(-100)
            },
            new AntivirusResultEvent
            {
                SubmissionId = submissionId,
                FileId = invalidFile,
                BlobName = "blob-invalid",
                Created = now.AddMinutes(-99)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-invalid",
                DataCount = 1,
                IsValid = false,
                ErrorCount = 1,
                Created = now.AddMinutes(-98)
            },
            new ProducerValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-invalid",
                IsValid = false,
                ErrorCount = 1,
                Created = now.AddMinutes(-97)
            },

            // The valid file, submitted, then the fee viewed and paid again.
            new AntivirusCheckEvent
            {
                SubmissionId = submissionId,
                FileType = FileType.Pom,
                FileId = finalFile,
                Created = now.AddMinutes(-60)
            },
            new AntivirusResultEvent
            {
                SubmissionId = submissionId,
                FileId = finalFile,
                BlobName = "blob-final",
                Created = now.AddMinutes(-59)
            },
            new CheckSplitterValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-final",
                DataCount = 1,
                IsValid = true,
                Created = now.AddMinutes(-58)
            },
            new ProducerValidationEvent
            {
                SubmissionId = submissionId,
                BlobName = "blob-final",
                IsValid = true,
                Created = now.AddMinutes(-57)
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
                FileId = finalFile,
                SubmittedBy = "Test User",
                Created = now.AddMinutes(-50)
            },
            new PackagingResubmissionFeeViewCreatedEvent
            {
                SubmissionId = submissionId,
                IsPackagingResubmissionFeeViewed = true,
                Created = now.AddMinutes(-40)
            },
            new PackagingDataResubmissionFeePaymentEvent
            {
                SubmissionId = submissionId,
                PaymentMethod = "PayByPhone",
                ReferenceNumber = "PEPR33333S01",
                Created = now.AddMinutes(-30)
            }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - the upload is complete and the fee is paid, so the frontend shows both steps as completed,
        // but the declaration step must still be outstanding.
        result.Should().NotBeNull();
        result.Value.First().ApplicationStatus.Should().Be(ApplicationStatusType.SubmittedToRegulator);
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR33333S01");
        result.Value.First().LastSubmittedFile!.FileId.Should().Be(finalFile);
        result.Value.First().ResubmissionFeePaymentMethod.Should().Be("PayByPhone");
        result.Value.First().IsResubmissionFeeViewed.Should().BeTrue();
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().BeNull();
        result.Value.First().ResubmissionApplicationSubmittedComment.Should().BeNull();
        result.Value.First().IsResubmitted.Should().BeNull();
    }

    // SUB-345: the same flow as the fixture above, stopped at the regulator's decision. This is the window
    // the two existing supersession checks both miss - no reference number has been raised since the
    // declaration (the frontend will not raise one while this response carries the first), and the ruled-on
    // file was submitted before the declaration that closed its cycle. The declaration must not be reported
    // here: the frontend reads it as "declared, awaiting the regulator" and routes past the page showing the
    // decision. The status must stay NotStarted so the closed cycle's upload and fee do not resolve as
    // completed against a file the user may need to replace.
    //
    // Accepted and Approved matter as much as Rejected: that page is where the user is told no resubmission
    // is needed, and skipping it drops them into a task list implying the opposite. The sibling test below
    // covers the decisions that page cannot speak to.
    [TestMethod]
    [DataRow(RegulatorDecision.Rejected)]
    [DataRow(RegulatorDecision.Accepted)]
    [DataRow(RegulatorDecision.Approved)]
    public async Task Handle_ShouldNotReportDeclarationButKeepTheCycleNotStarted_WhenTheRegulatorHasRuledOnTheDeclaredCycle(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var originalFile = Guid.NewGuid();
        var rejectedFile = Guid.NewGuid();
        var now = DateTime.Now;

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            // The original submission, accepted by the regulator.
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = originalFile, Created = now.AddMinutes(-200) },
            new SubmittedEvent { SubmissionId = submissionId, FileId = originalFile, Created = now.AddMinutes(-190) },
            new RegulatorPoMDecisionEvent { SubmissionId = submissionId, Decision = RegulatorDecision.Accepted, Created = now.AddMinutes(-180) },

            // The first resubmission, declared and paid for: the only reference number this submission gets.
            new PackagingResubmissionReferenceNumberCreatedEvent { SubmissionId = submissionId, PackagingResubmissionReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-170) },
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = rejectedFile, Created = now.AddMinutes(-160) },
            new AntivirusResultEvent { SubmissionId = submissionId, FileId = rejectedFile, BlobName = "blob-rejected", Created = now.AddMinutes(-159) },
            new CheckSplitterValidationEvent { SubmissionId = submissionId, BlobName = "blob-rejected", DataCount = 1, IsValid = true, Created = now.AddMinutes(-158) },
            new ProducerValidationEvent { SubmissionId = submissionId, BlobName = "blob-rejected", IsValid = true, Created = now.AddMinutes(-157) },
            new SubmittedEvent { SubmissionId = submissionId, FileId = rejectedFile, Created = now.AddMinutes(-150) },
            new PackagingResubmissionFeeViewCreatedEvent { SubmissionId = submissionId, IsPackagingResubmissionFeeViewed = true, Created = now.AddMinutes(-145) },
            new PackagingDataResubmissionFeePaymentEvent { SubmissionId = submissionId, PaymentMethod = "PayByPhone", ReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-140) },
            new PackagingResubmissionApplicationSubmittedCreatedEvent { SubmissionId = submissionId, IsResubmitted = true, SubmissionDate = now.AddMinutes(-135), Comments = "First resubmission", Created = now.AddMinutes(-135) },

            // The regulator rules on the declared cycle. The user has done nothing since.
            new RegulatorPoMDecisionEvent { SubmissionId = submissionId, Decision = decision, IsResubmissionRequired = decision == RegulatorDecision.Rejected, Created = now.AddMinutes(-120) }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().ApplicationStatus.Should().Be(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR33333S01");
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().BeNull();
        result.Value.First().ResubmissionApplicationSubmittedComment.Should().BeNull();
        result.Value.First().IsResubmitted.Should().BeNull();

        // The fee view and payment belong to the cycle the decision closed, so they age out with the
        // declaration. Left reported alongside a suppressed declaration, the frontend read them as "fee
        // viewed, not yet declared" and the sub-landing tile told the user to submit to the regulator while
        // the task list showed every step unstarted.
        result.Value.First().IsResubmissionFeeViewed.Should().BeNull();
        result.Value.First().ResubmissionFeePaymentMethod.Should().BeNull();
    }

    // SUB-345: the same fixture as above with a decision UploadNewFileToSubmit has no wording for. Suppressing
    // the declaration would send the user to that page, where Cancelled, Queried and None all fall through
    // every decision branch to a bare "you already submitted a file" and the regulator's comment is never
    // rendered - a worse stop than not stopping at all. So these keep reporting the declaration, which leaves
    // ResubmissionApplicationSubmitted true and routes the frontend straight to the task list, unchanged from
    // the behaviour before this fix.
    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.None)]
    public async Task Handle_ShouldStillReportDeclaration_WhenTheDecisionIsOneTheInterstitialCannotExplain(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var originalFile = Guid.NewGuid();
        var ruledOnFile = Guid.NewGuid();
        var now = DateTime.Now;
        var submissionDate = now.AddMinutes(-135);

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            // The original submission, accepted by the regulator.
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = originalFile, Created = now.AddMinutes(-200) },
            new SubmittedEvent { SubmissionId = submissionId, FileId = originalFile, Created = now.AddMinutes(-190) },
            new RegulatorPoMDecisionEvent { SubmissionId = submissionId, Decision = RegulatorDecision.Accepted, Created = now.AddMinutes(-180) },

            // The first resubmission, declared and paid for: the only reference number this submission gets.
            new PackagingResubmissionReferenceNumberCreatedEvent { SubmissionId = submissionId, PackagingResubmissionReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-170) },
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = ruledOnFile, Created = now.AddMinutes(-160) },
            new AntivirusResultEvent { SubmissionId = submissionId, FileId = ruledOnFile, BlobName = "blob-ruled-on", Created = now.AddMinutes(-159) },
            new CheckSplitterValidationEvent { SubmissionId = submissionId, BlobName = "blob-ruled-on", DataCount = 1, IsValid = true, Created = now.AddMinutes(-158) },
            new ProducerValidationEvent { SubmissionId = submissionId, BlobName = "blob-ruled-on", IsValid = true, Created = now.AddMinutes(-157) },
            new SubmittedEvent { SubmissionId = submissionId, FileId = ruledOnFile, Created = now.AddMinutes(-150) },
            new PackagingResubmissionFeeViewCreatedEvent { SubmissionId = submissionId, IsPackagingResubmissionFeeViewed = true, Created = now.AddMinutes(-145) },
            new PackagingDataResubmissionFeePaymentEvent { SubmissionId = submissionId, PaymentMethod = "PayByPhone", ReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-140) },
            new PackagingResubmissionApplicationSubmittedCreatedEvent { SubmissionId = submissionId, IsResubmitted = true, SubmissionDate = submissionDate, Comments = "First resubmission", Created = submissionDate },

            // A decision the frontend has no page to show. The user has done nothing since.
            new RegulatorPoMDecisionEvent { SubmissionId = submissionId, Decision = decision, Created = now.AddMinutes(-120) }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR33333S01");
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().Be(submissionDate);
        result.Value.First().ResubmissionApplicationSubmittedComment.Should().Be("First resubmission");
        result.Value.First().IsResubmitted.Should().BeTrue();

        // The fee events belong to the same cycle as the declaration, so a decision that does not close the
        // cycle must not age them out either. Reporting all three together is what keeps the task list showing
        // this cycle as declared and paid for.
        result.Value.First().IsResubmissionFeeViewed.Should().BeTrue();
        result.Value.First().ResubmissionFeePaymentMethod.Should().Be("PayByPhone");
    }

    // SUB-345: the counterpart - a declaration the regulator has not yet ruled on is the frontend's
    // "declared, awaiting the regulator" state and must still be reported. Only a decision that lands after
    // the declaration supersedes it; the earlier decision that prompted this cycle does not.
    [TestMethod]
    public async Task Handle_ShouldStillReportDeclaration_WhenTheOnlyRegulatorDecisionPredatesIt()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var originalFile = Guid.NewGuid();
        var declaredFile = Guid.NewGuid();
        var now = DateTime.Now;
        var submissionDate = now.AddMinutes(-135);

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = originalFile, Created = now.AddMinutes(-200) },
            new SubmittedEvent { SubmissionId = submissionId, FileId = originalFile, Created = now.AddMinutes(-190) },
            new RegulatorPoMDecisionEvent { SubmissionId = submissionId, Decision = RegulatorDecision.Rejected, IsResubmissionRequired = true, Created = now.AddMinutes(-180) },

            new PackagingResubmissionReferenceNumberCreatedEvent { SubmissionId = submissionId, PackagingResubmissionReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-170) },
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = declaredFile, Created = now.AddMinutes(-160) },
            new AntivirusResultEvent { SubmissionId = submissionId, FileId = declaredFile, BlobName = "blob-declared", Created = now.AddMinutes(-159) },
            new CheckSplitterValidationEvent { SubmissionId = submissionId, BlobName = "blob-declared", DataCount = 1, IsValid = true, Created = now.AddMinutes(-158) },
            new ProducerValidationEvent { SubmissionId = submissionId, BlobName = "blob-declared", IsValid = true, Created = now.AddMinutes(-157) },
            new SubmittedEvent { SubmissionId = submissionId, FileId = declaredFile, Created = now.AddMinutes(-150) },
            new PackagingDataResubmissionFeePaymentEvent { SubmissionId = submissionId, PaymentMethod = "PayByPhone", ReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-140) },

            // Declared, and the regulator has not ruled on it yet.
            new PackagingResubmissionApplicationSubmittedCreatedEvent { SubmissionId = submissionId, IsResubmitted = true, SubmissionDate = submissionDate, Comments = "Awaiting the regulator", Created = submissionDate }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().Be(submissionDate);
        result.Value.First().ResubmissionApplicationSubmittedComment.Should().Be("Awaiting the regulator");
        result.Value.First().IsResubmitted.Should().BeTrue();
    }

    // SUB-345: the same fixture as the ruled-on tests above, read from the other side. Every current-cycle field
    // is correctly aged out by the decision, and on its own that leaves a completed resubmission looking exactly
    // like one that was never started - which is what had the sub-landing tile offering to resubmit data the
    // regulator had already accepted. The closed cycle is reported separately so the frontend can show it.
    [TestMethod]
    [DataRow(RegulatorDecision.Rejected)]
    [DataRow(RegulatorDecision.Accepted)]
    [DataRow(RegulatorDecision.Approved)]
    public async Task Handle_ShouldReportTheClosedCycle_WhenTheRegulatorHasRuledOnTheDeclaredCycle(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var originalFile = Guid.NewGuid();
        var ruledOnFile = Guid.NewGuid();
        var now = DateTime.Now;
        var declarationDate = now.AddMinutes(-135);
        var decisionDate = now.AddMinutes(-120);

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            // The original submission, accepted by the regulator (SUB-345).
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = originalFile, FileName = "original.csv", Created = now.AddMinutes(-200) },
            new SubmittedEvent { SubmissionId = submissionId, FileId = originalFile, Created = now.AddMinutes(-190) },
            new RegulatorPoMDecisionEvent { SubmissionId = submissionId, Decision = RegulatorDecision.Accepted, FileId = originalFile, Created = now.AddMinutes(-180) },

            // A resubmission taken all the way through: uploaded, submitted, fee viewed and paid, declared (SUB-345).
            new PackagingResubmissionReferenceNumberCreatedEvent { SubmissionId = submissionId, PackagingResubmissionReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-170) },
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = ruledOnFile, FileName = "resubmission.csv", Created = now.AddMinutes(-160) },
            new AntivirusResultEvent { SubmissionId = submissionId, FileId = ruledOnFile, BlobName = "blob-ruled-on", Created = now.AddMinutes(-159) },
            new CheckSplitterValidationEvent { SubmissionId = submissionId, BlobName = "blob-ruled-on", DataCount = 1, IsValid = true, Created = now.AddMinutes(-158) },
            new ProducerValidationEvent { SubmissionId = submissionId, BlobName = "blob-ruled-on", IsValid = true, Created = now.AddMinutes(-157) },
            new SubmittedEvent { SubmissionId = submissionId, FileId = ruledOnFile, SubmittedBy = "Anwen Prydderch", Created = now.AddMinutes(-150) },
            new PackagingResubmissionFeeViewCreatedEvent { SubmissionId = submissionId, IsPackagingResubmissionFeeViewed = true, Created = now.AddMinutes(-145) },
            new PackagingDataResubmissionFeePaymentEvent { SubmissionId = submissionId, PaymentMethod = "PayByPhone", ReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-140) },
            new PackagingResubmissionApplicationSubmittedCreatedEvent { SubmissionId = submissionId, IsResubmitted = true, SubmittedBy = "Anwen Prydderch", SubmissionDate = declarationDate, Comments = "First resubmission", Created = declarationDate },

            // The regulator rules on it. The user has done nothing since (SUB-345).
            new RegulatorPoMDecisionEvent { SubmissionId = submissionId, Decision = decision, IsResubmissionRequired = decision == RegulatorDecision.Rejected, FileId = ruledOnFile, Comments = "Regulator comment", RegistrationReferenceNumber = "PEPR33333S01R", Created = decisionDate }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - the current-cycle fields stay exactly as the ruled-on tests above require
        result.Should().NotBeNull();
        result.Value.First().ApplicationStatus.Should().Be(ApplicationStatusType.NotStarted);
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().BeNull();
        result.Value.First().ResubmissionFeePaymentMethod.Should().BeNull();

        // Assert - and the cycle the decision closed is described in full
        var completed = result.Value.First().LastCompletedResubmission;
        completed.Should().NotBeNull();
        completed!.ApplicationReferenceNumber.Should().Be("PEPR33333S01");
        completed.ResubmissionReferenceNumber.Should().Be("PEPR33333S01R");
        completed.DeclarationDate.Should().Be(declarationDate);
        completed.DeclarationComment.Should().Be("First resubmission");
        completed.DeclaredByName.Should().Be("Anwen Prydderch");
        completed.IsResubmissionFeeViewed.Should().BeTrue();
        completed.ResubmissionFeePaymentMethod.Should().Be("PayByPhone");
        completed.Decision.Should().Be(decision.ToString());
        completed.RegulatorComments.Should().Be("Regulator comment");
        completed.DecisionDate.Should().Be(decisionDate);

        // The file named by the decision, not whichever file is newest by the time the user looks (SUB-345)
        completed.FileName.Should().Be("resubmission.csv");
        completed.SubmittedFile!.FileId.Should().Be(ruledOnFile);
        completed.SubmittedFile.SubmittedByName.Should().Be("Anwen Prydderch");
        completed.SubmittedFile.SubmittedDateTime.Should().Be(now.AddMinutes(-150));
    }

    // SUB-345: Cancelled, Queried and None do not close a cycle - their declaration keeps being reported and
    // the frontend keeps treating the cycle as live - so there is no completed cycle to report either.
    [TestMethod]
    [DataRow(RegulatorDecision.Cancelled)]
    [DataRow(RegulatorDecision.Queried)]
    [DataRow(RegulatorDecision.None)]
    public async Task Handle_ShouldNotReportAClosedCycle_WhenTheDecisionIsOneTheInterstitialCannotExplain(RegulatorDecision decision)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var ruledOnFile = Guid.NewGuid();
        var now = DateTime.Now;
        var declarationDate = now.AddMinutes(-135);

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            new PackagingResubmissionReferenceNumberCreatedEvent { SubmissionId = submissionId, PackagingResubmissionReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-170) },
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = ruledOnFile, FileName = "resubmission.csv", Created = now.AddMinutes(-160) },
            new AntivirusResultEvent { SubmissionId = submissionId, FileId = ruledOnFile, BlobName = "blob-ruled-on", Created = now.AddMinutes(-159) },
            new CheckSplitterValidationEvent { SubmissionId = submissionId, BlobName = "blob-ruled-on", DataCount = 1, IsValid = true, Created = now.AddMinutes(-158) },
            new ProducerValidationEvent { SubmissionId = submissionId, BlobName = "blob-ruled-on", IsValid = true, Created = now.AddMinutes(-157) },
            new SubmittedEvent { SubmissionId = submissionId, FileId = ruledOnFile, Created = now.AddMinutes(-150) },
            new PackagingDataResubmissionFeePaymentEvent { SubmissionId = submissionId, PaymentMethod = "PayByPhone", ReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-140) },
            new PackagingResubmissionApplicationSubmittedCreatedEvent { SubmissionId = submissionId, IsResubmitted = true, SubmissionDate = declarationDate, Created = declarationDate },
            new RegulatorPoMDecisionEvent { SubmissionId = submissionId, Decision = decision, FileId = ruledOnFile, Created = now.AddMinutes(-120) }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().Be(declarationDate);
        result.Value.First().LastCompletedResubmission.Should().BeNull();
    }

    // SUB-345: a declaration still awaiting its decision is a live cycle, not a completed one. The earlier
    // decision that prompted the resubmission ruled on the file before it, so it closes nothing here.
    [TestMethod]
    public async Task Handle_ShouldNotReportAClosedCycle_WhenTheOnlyRegulatorDecisionPredatesTheDeclaration()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var originalFile = Guid.NewGuid();
        var declaredFile = Guid.NewGuid();
        var now = DateTime.Now;
        var declarationDate = now.AddMinutes(-135);

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = originalFile, Created = now.AddMinutes(-200) },
            new SubmittedEvent { SubmissionId = submissionId, FileId = originalFile, Created = now.AddMinutes(-190) },
            new RegulatorPoMDecisionEvent { SubmissionId = submissionId, Decision = RegulatorDecision.Rejected, IsResubmissionRequired = true, FileId = originalFile, Created = now.AddMinutes(-180) },

            new PackagingResubmissionReferenceNumberCreatedEvent { SubmissionId = submissionId, PackagingResubmissionReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-170) },
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = declaredFile, Created = now.AddMinutes(-160) },
            new AntivirusResultEvent { SubmissionId = submissionId, FileId = declaredFile, BlobName = "blob-declared", Created = now.AddMinutes(-159) },
            new CheckSplitterValidationEvent { SubmissionId = submissionId, BlobName = "blob-declared", DataCount = 1, IsValid = true, Created = now.AddMinutes(-158) },
            new ProducerValidationEvent { SubmissionId = submissionId, BlobName = "blob-declared", IsValid = true, Created = now.AddMinutes(-157) },
            new SubmittedEvent { SubmissionId = submissionId, FileId = declaredFile, Created = now.AddMinutes(-150) },
            new PackagingDataResubmissionFeePaymentEvent { SubmissionId = submissionId, PaymentMethod = "PayByPhone", ReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-140) },
            new PackagingResubmissionApplicationSubmittedCreatedEvent { SubmissionId = submissionId, IsResubmitted = true, SubmissionDate = declarationDate, Created = declarationDate }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.First().LastCompletedResubmission.Should().BeNull();
    }

    // SUB-345: starting another resubmission does not undo the one before it - the current-cycle fields follow
    // the new cycle while the closed cycle keeps its own reference number, fee and file.
    [TestMethod]
    public async Task Handle_ShouldKeepReportingTheClosedCycle_WhenANewCycleHasBeenStartedSinceTheRuling()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var ruledOnFile = Guid.NewGuid();
        var newFile = Guid.NewGuid();
        var now = DateTime.Now;
        var declarationDate = now.AddMinutes(-135);

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriods = new List<string> { "January - June 2024 - TEST" }
        };

        var submission = BuildSubmission(submissionId, query, complianceSchemeId: null);

        var events = new List<AbstractSubmissionEvent>
        {
            // A resubmission taken all the way through and accepted (SUB-345).
            new PackagingResubmissionReferenceNumberCreatedEvent { SubmissionId = submissionId, PackagingResubmissionReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-170) },
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = ruledOnFile, FileName = "accepted.csv", Created = now.AddMinutes(-160) },
            new AntivirusResultEvent { SubmissionId = submissionId, FileId = ruledOnFile, BlobName = "blob-accepted", Created = now.AddMinutes(-159) },
            new CheckSplitterValidationEvent { SubmissionId = submissionId, BlobName = "blob-accepted", DataCount = 1, IsValid = true, Created = now.AddMinutes(-158) },
            new ProducerValidationEvent { SubmissionId = submissionId, BlobName = "blob-accepted", IsValid = true, Created = now.AddMinutes(-157) },
            new SubmittedEvent { SubmissionId = submissionId, FileId = ruledOnFile, Created = now.AddMinutes(-150) },
            new PackagingResubmissionFeeViewCreatedEvent { SubmissionId = submissionId, IsPackagingResubmissionFeeViewed = true, Created = now.AddMinutes(-145) },
            new PackagingDataResubmissionFeePaymentEvent { SubmissionId = submissionId, PaymentMethod = "PayByPhone", ReferenceNumber = "PEPR33333S01", Created = now.AddMinutes(-140) },
            new PackagingResubmissionApplicationSubmittedCreatedEvent { SubmissionId = submissionId, IsResubmitted = true, SubmissionDate = declarationDate, Comments = "First resubmission", Created = declarationDate },
            new RegulatorPoMDecisionEvent { SubmissionId = submissionId, Decision = RegulatorDecision.Accepted, FileId = ruledOnFile, Created = now.AddMinutes(-120) },

            // A second cycle, opened and uploaded to but not yet submitted (SUB-345).
            new PackagingResubmissionReferenceNumberCreatedEvent { SubmissionId = submissionId, PackagingResubmissionReferenceNumber = "PEPR33333S02", Created = now.AddMinutes(-110) },
            new AntivirusCheckEvent { SubmissionId = submissionId, FileType = FileType.Pom, FileId = newFile, FileName = "second-attempt.csv", Created = now.AddMinutes(-100) },
            new AntivirusResultEvent { SubmissionId = submissionId, FileId = newFile, BlobName = "blob-second", Created = now.AddMinutes(-99) },
            new CheckSplitterValidationEvent { SubmissionId = submissionId, BlobName = "blob-second", DataCount = 1, IsValid = true, Created = now.AddMinutes(-98) },
            new ProducerValidationEvent { SubmissionId = submissionId, BlobName = "blob-second", IsValid = true, Created = now.AddMinutes(-97) }
        };

        SetupMocks(submission, events);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - the new cycle owns the current-cycle fields
        result.Should().NotBeNull();
        result.Value.First().ApplicationStatus.Should().Be(ApplicationStatusType.FileUploaded);
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR33333S02");
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().BeNull();
        result.Value.First().ResubmissionFeePaymentMethod.Should().BeNull();

        // Assert - the accepted cycle is still described, under its own reference number
        var completed = result.Value.First().LastCompletedResubmission;
        completed.Should().NotBeNull();
        completed!.ApplicationReferenceNumber.Should().Be("PEPR33333S01");
        completed.DeclarationDate.Should().Be(declarationDate);
        completed.ResubmissionFeePaymentMethod.Should().Be("PayByPhone");
        completed.Decision.Should().Be(RegulatorDecision.Accepted.ToString());
        completed.FileName.Should().Be("accepted.csv");
        completed.SubmittedFile!.FileId.Should().Be(ruledOnFile);
    }

    private static Submission BuildSubmission(Guid submissionId, GetPackagingResubmissionApplicationDetailsQuery query, Guid? complianceSchemeId) =>
        new()
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriods.First(),
            Created = DateTime.Now,
            IsSubmitted = true,
            AppReferenceNumber = "TestRef"
        };

    private void SetupMocks(Submission submission, List<AbstractSubmissionEvent> events)
    {
        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        _validationErrorQueryRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationError, bool>>>()))
            .Returns(new List<AbstractValidationError>().BuildMock);

        _validationWarningRepositoryMock
            .Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractValidationWarning, bool>>>()))
            .Returns(new List<AbstractValidationWarning>().BuildMock);
    }
}