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

    // SUB-332: previously asserted NotStarted. The reference-number event is not closed by any
    // application-submitted event, so the cycle is open and the status must reflect that.
    [TestMethod]
    public async Task Handle_ShouldReturnOpenCycle_WhenRegulatorPackagingDecisionEventisAcceptedAndUploadIsIncomplete()
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
        result.Value.First().ApplicationStatus.Should().NotBe(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationReferenceNumber.Should().Be("Test");
    }

    // SUB-332: previously asserted NotStarted. See the accepted-decision test above.
    [TestMethod]
    public async Task Handle_ShouldReturnOpenCycle_WhenRegulatorPackagingDecisionEventisApprovedAndUploadIsIncomplete()
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
        result.Value.First().ApplicationStatus.Should().NotBe(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationReferenceNumber.Should().Be("Test");
    }

    // SUB-332: previously asserted NotStarted. This is the incident's starting state - the regulator
    // rejected the submission and the user has entered the resubmission journey.
    [TestMethod]
    public async Task Handle_ShouldReturnOpenCycle_WhenRegulatorPackagingDecision_EventisRejectedByRegulator()
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
        result.Value.First().ApplicationStatus.Should().NotBe(ApplicationStatusType.NotStarted);
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

    // SUB-332: the cycle is identified by the earliest open PackagingResubmissionReferenceNumberCreated
    // event, so upload validity and recency must not be able to resolve an open cycle as NotStarted.
    [TestMethod]
    public async Task Handle_ShouldReturnOpenCycle_WhenLaterUploadFailsValidationAfterAValidFileWasSubmitted()
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
        result.Value.First().ApplicationStatus.Should().NotBe(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR12345S01");
        result.Value.First().LastSubmittedFile!.FileId.Should().Be(fileA);
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_ShouldReturnOpenCycle_WhenNothingHasBeenUploadedSinceTheReferenceNumberWasCreated()
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
        result.Value.First().ApplicationStatus.Should().NotBe(ApplicationStatusType.NotStarted);
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
        result.Value.First().ApplicationStatus.Should().NotBe(ApplicationStatusType.NotStarted);
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
        result.Value.First().ApplicationStatus.Should().NotBe(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationReferenceNumber.Should().Be("PEPR12345S02");
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().BeNull();
        result.Value.First().ResubmissionApplicationSubmittedComment.Should().BeNull();
        result.Value.First().IsResubmitted.Should().BeNull();
    }

    [TestMethod]
    public async Task Handle_ShouldReturnOpenCycle_WhenLaterUploadFailsValidation_ForComplianceSchemePath()
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
        result.Value.First().ApplicationStatus.Should().NotBe(ApplicationStatusType.NotStarted);
        result.Value.First().ApplicationStatus.ToString().Should().Be("SubmittedToRegulator");
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

    // SUB-332: a closed cycle was closed by a declaration, so that declaration is always the one to report.
    // Deriving this from cycle membership rather than declaration-vs-submit ordering keeps
    // ApplicationReferenceNumber and ResubmissionApplicationSubmittedDate consistent with one another, which
    // the frontend's in-progress check relies on.
    [TestMethod]
    public async Task Handle_ShouldReportDeclaration_WhenClosedCycleDeclarationPredatesTheLastSubmit()
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
        result.Value.First().ResubmissionApplicationSubmittedDate.Should().NotBeNull();
        result.Value.First().ResubmissionApplicationSubmittedComment.Should().Be("Declared");
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