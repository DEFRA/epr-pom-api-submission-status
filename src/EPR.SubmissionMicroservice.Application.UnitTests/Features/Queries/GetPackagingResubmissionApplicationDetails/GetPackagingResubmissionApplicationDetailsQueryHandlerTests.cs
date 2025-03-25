using EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

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
            SubmissionPeriod = "January - June 2024 - TEST"
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
        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "January - June 2024 - TEST"
        };

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = query.SubmissionPeriod,
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
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.IsSubmitted.Should().BeFalse();
        result.Value.ApplicationReferenceNumber.Should().Be("test");
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
             SubmissionPeriod = "January - June 2024 - TEST",
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
         result.Value.SubmissionId.Should().Be(submission.Id);
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
            SubmissionPeriod = "January - June 2024 - TEST",
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
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("FileUploaded");
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
            SubmissionPeriod = "January - June 2024 - TEST",
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
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("FileUploaded");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnAcceptedApplicationStatusType_WhenRegulatorPackagingDecisionEventisAccepted()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "January - June 2024 - TEST",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriod,
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
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnApprovedByRegulator_WhenRegulatorPackagingDecisionEventisApproved()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "January - June 2024 - TEST",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriod,
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
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
    }

    [TestMethod]
    public async Task Handle_ShouldReturnRejectedByRegulator_WhenRegulatorPackagingDecision_EventisRejectedByRegulator()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var query = new GetPackagingResubmissionApplicationDetailsQuery
        {
            OrganisationId = Guid.NewGuid(),
            SubmissionPeriod = "January - June 2024 - TEST",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriod,
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
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
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
            SubmissionPeriod = "January - June 2024 - TEST",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriod,
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
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ResubmissionFeePaymentMethod.Should().Be(null);
        result.Value.ResubmissionApplicationSubmittedComment.Should().BeNull();
        result.Value.ResubmissionApplicationSubmittedDate.Should().BeNull();
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
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
            SubmissionPeriod = "January - June 2024 - TEST",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriod,
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
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
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
            SubmissionPeriod = "January - June 2024 - TEST",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriod,
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
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
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
            SubmissionPeriod = "January - June 2024 - TEST",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriod,
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
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
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
            SubmissionPeriod = "January - June 2024 - TEST",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriod,
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
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
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
            SubmissionPeriod = "January - June 2024 - TEST",
            ComplianceSchemeId = complianceSchemeId
        };

        var submission = new Submission
        {
            Id = submissionId,
            ComplianceSchemeId = complianceSchemeId,
            OrganisationId = query.OrganisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = query.SubmissionPeriod,
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
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("FileUploaded");
        result.Value.ResubmissionFeePaymentMethod!.Should().Be("PayByPhone");
    }
}