using EPR.SubmissionMicroservice.Application.Features.Queries.GetRegistrationApplicationDetails;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.Submission;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.GetPackagingResubmissionApplicationDetails;

[TestClass]
public class GetPackagingResubmissionApplicationDetailsQueryHandlerTests
{
    private readonly Mock<IQueryRepository<Submission>> _submissionQueryRepositoryMock;
    private readonly Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventQueryRepositoryMock;
    private readonly Mock<IQueryRepository<AbstractValidationError>> _validationErrorQueryRepositoryMock;
    private readonly GetPackagingResubmissionApplicationDetailsQueryHandler _handler;

    public GetPackagingResubmissionApplicationDetailsQueryHandlerTests()
    {
        _submissionQueryRepositoryMock = new Mock<IQueryRepository<Submission>>();
        _submissionEventQueryRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _submissionEventQueryRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();
        _validationErrorQueryRepositoryMock = new Mock<IQueryRepository<AbstractValidationError>>();

        _handler = new GetPackagingResubmissionApplicationDetailsQueryHandler(
            _submissionQueryRepositoryMock.Object,
            _submissionEventQueryRepositoryMock.Object,
            _validationErrorQueryRepositoryMock.Object);
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
                FileType = FileType.CompanyDetails,
                SubmissionId = submissionId,
                Created = DateTime.Now.AddMinutes(-5),
                FileId = fileId,
            },
            new AntivirusResultEvent
            {
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
                SubmissionId = submissionId,
                DataCount = 1,
            },
            new ProducerValidationEvent
            {
                SubmissionId = submissionId,
                IsValid = false,
                ErrorCount = 2,
                WarningCount = 2,
                Created = DateTime.Now.AddMinutes(-5)
            },
            new ProducerValidationEvent
            {
                SubmissionId = submissionId,
                IsValid = false,
                ErrorCount = 1,
                WarningCount = 1,
                Created = DateTime.Now.AddMinutes(-5)
            },
            new ProducerValidationEvent
            {
                SubmissionId = submissionId,
                IsValid = true,
            },
            new SubmittedEvent
            {
                SubmissionId = submissionId,
            },
            new RegulatorPoMDecisionEvent
            {
                Decision = RegulatorDecision.Rejected,
                IsResubmissionRequired = true
            }
        };

        _submissionQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<Submission, bool>>>()))
            .Returns(new[] { submission }.BuildMock());

        _submissionEventQueryRepositoryMock.Setup(repo => repo.GetAll(It.IsAny<Expression<Func<AbstractSubmissionEvent, bool>>>()))
            .Returns<Expression<Func<AbstractSubmissionEvent, bool>>>(expr => events.Where(expr.Compile()).BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.SubmissionId.Should().Be(submission.Id);
        result.Value.ApplicationStatus.ToString().Should().Be("SubmittedAndHasRecentFileUpload");
    }
}