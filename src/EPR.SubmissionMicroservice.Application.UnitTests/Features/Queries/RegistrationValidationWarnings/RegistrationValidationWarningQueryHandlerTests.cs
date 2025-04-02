using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Application.Features.Queries.RegisterationValidationWarnings;
using EPR.SubmissionMicroservice.Application.Options;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.RegistrationValidationWarnings;

[TestClass]
public class RegistrationValidationWarningQueryHandlerTests
{
    private RegistrationValidationWarningQueryHandler _systemUnderTest;
    private Mock<IQueryRepository<AbstractValidationWarning>> _validationEventWarningQueryRepositoryMock;
    private Mock<IValidationEventHelper> _validationEventHelperMock;

    [TestInitialize]
    public void TestInitialize()
    {
        var validationOptions = new ValidationOptions { MaxIssuesToProcess = 1 };
        _validationEventWarningQueryRepositoryMock = new Mock<IQueryRepository<AbstractValidationWarning>>();
        _validationEventHelperMock = new Mock<IValidationEventHelper>();

        _systemUnderTest = new RegistrationValidationWarningQueryHandler(
            _validationEventWarningQueryRepositoryMock.Object,
            _validationEventHelperMock.Object,
            AutoMapperHelpers.GetMapper(),
            Microsoft.Extensions.Options.Options.Create(validationOptions));
    }

    [TestMethod]
    public async Task Handler_ReturnsNoWarnings_WhenNoEventsExist()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();

        var request = new RegistrationValidationWarningQuery(submissionId, organisationId);

        _validationEventHelperMock.
            Setup(x => x.GetLatestAntivirusResult(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AntivirusResultEvent?)null);

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(0);
    }

    [TestMethod]
    public async Task Handler_ReturnsWarnings_WhenAllValidationWarningEventsExist()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var blobName = Guid.NewGuid().ToString();
        var producerValidationEventId = Guid.NewGuid();

        var antivirusResult = new AntivirusResultEvent()
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            FileId = fileId,
            AntivirusScanResult = AntivirusScanResult.Success,
            BlobName = blobName
        };

        var producerValidationWarningQueryEvents = new List<AbstractValidationWarning>
        {
            new ProducerValidationWarning
            {
                ValidationEventId = producerValidationEventId,
                BlobName = blobName
            }
        };

        var request = new RegistrationValidationWarningQuery(submissionId, organisationId);

        _validationEventHelperMock.
            Setup(x => x.GetLatestAntivirusResult(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResult);

        _validationEventWarningQueryRepositoryMock
            .Setup(x => x.GetAll(It.Is<Expression<Func<AbstractValidationWarning, bool>>>(y => y.Compile().Invoke(new RegistrationValidationWarning() { BlobName = blobName }))))
            .Returns(GenerateRegistrationValidationWarningQueryEventMock);

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);

        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
           .As<RegistrationValidationIssueGetResponse>().ColumnErrors.Count.Should().Be(2);
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().ColumnErrors[0].ErrorCode.Should().Be("840");
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().ColumnErrors[0].ColumnIndex.Should().Be(3);
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().ColumnErrors[0].ColumnName.Should().Be("trading_name");
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().OrganisationId.Should().Be("999");
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().SubsidiaryId.Should().Be("5655");
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().RowNumber.Should().Be(4);
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().ErrorCodes.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Handler_ReturnsOneError_WhenMaxErrorsReached()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var blobName = Guid.NewGuid().ToString();
        var producerValidationEventId = Guid.NewGuid();

        var antivirusResult = new AntivirusResultEvent()
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            FileId = fileId,
            AntivirusScanResult = AntivirusScanResult.Success,
            BlobName = blobName
        };

        var producerValidationErrorQueryEvents = new List<AbstractValidationWarning>
        {
            new ProducerValidationWarning
            {
                ValidationEventId = producerValidationEventId,
                BlobName = blobName,
                ErrorCodes = new() { "51", "53" }
            },
            new ProducerValidationWarning
            {
                ValidationEventId = producerValidationEventId,
                BlobName = blobName,
                ErrorCodes = new() { "71" }
            }
        };

        var request = new RegistrationValidationWarningQuery(submissionId, organisationId);

        _validationEventHelperMock.
            Setup(x => x.GetLatestAntivirusResult(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResult);

        _validationEventWarningQueryRepositoryMock
            .Setup(x => x.GetAll(It.Is<Expression<Func<AbstractValidationWarning, bool>>>(y => y.Compile().Invoke(new RegistrationValidationWarning { BlobName = blobName }))))
            .Returns(producerValidationErrorQueryEvents.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
    }

    private IQueryable<AbstractValidationWarning> GenerateRegistrationValidationWarningQueryEventMock()
    {
        return new List<AbstractValidationWarning>
        {
            new RegistrationValidationWarning
            {
                 ColumnErrors = new List<ColumnValidationError>
                 {
                     new()
                     {
                         ErrorCode = "840",
                         ColumnIndex = 3,
                         ColumnName = "trading_name"
                     },
                     new()
                     {
                         ErrorCode = "819",
                         ColumnIndex = 6,
                         ColumnName = "main_activity_sic"
                     },
                 },
                 OrganisationId = "999",
                 SubsidiaryId = "5655",
                 RowNumber = 4,
                 ErrorCodes = { }
            }
        }.BuildMock();
    }
}
