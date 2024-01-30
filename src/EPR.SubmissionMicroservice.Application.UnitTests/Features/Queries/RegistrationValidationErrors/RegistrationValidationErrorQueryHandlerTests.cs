using EPR.SubmissionMicroservice.Application.Features.Queries.Common;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Application.Features.Queries.RegistrationValidationErrors;
using EPR.SubmissionMicroservice.Application.Options;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventError;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.RegistrationValidationErrors;

[TestClass]
public class RegistrationValidationErrorQueryHandlerTests
{
    private RegistrationValidationErrorQueryHandler _systemUnderTest;
    private Mock<IQueryRepository<AbstractValidationError>> _validationEventErrorQueryRepositoryMock;
    private Mock<IValidationEventHelper> _validationEventHelperMock;

    [TestInitialize]
    public void TestInitialize()
    {
        var validationOptions = new ValidationOptions { MaxIssuesToProcess = 1 };
        _validationEventErrorQueryRepositoryMock = new Mock<IQueryRepository<AbstractValidationError>>();
        _validationEventHelperMock = new Mock<IValidationEventHelper>();
        _systemUnderTest = new RegistrationValidationErrorQueryHandler(
            Microsoft.Extensions.Options.Options.Create(validationOptions),
            _validationEventErrorQueryRepositoryMock.Object,
            _validationEventHelperMock.Object,
            AutoMapperHelpers.GetMapper());
    }

    [TestMethod]
    public async Task Handle_ReturnsNoErrors_WhenNoEventsExist()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();

        var request = new RegistrationValidationErrorQuery(submissionId, organisationId);

        _validationEventHelperMock.
            Setup(x => x.GetLatestAntivirusResult(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AntivirusResultEvent?)null);

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(0);
    }

    [TestMethod]
    public async Task Handle_ReturnsError_WhenAllValidationErrorEventsExist()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var blobName = Guid.NewGuid().ToString();

        var antivirusResult = GenerateAntiVirusResult(submissionId, fileId, blobName);

        var request = new RegistrationValidationErrorQuery(submissionId, organisationId);

        _validationEventHelperMock.
            Setup(x => x.GetLatestAntivirusResult(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResult);

        _validationEventErrorQueryRepositoryMock
            .Setup(x => x.GetAll(It.Is<Expression<Func<AbstractValidationError, bool>>>(y => y.Compile().Invoke(new RegistrationValidationError() { BlobName = blobName }))))
            .Returns(GenerateRegistrationValidationErrorQueryEventMock);

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task Handle_ReturnsOneError_WhenMaxErrorsReached()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var blobName = Guid.NewGuid().ToString();
        var validationEventId = Guid.NewGuid();

        var antivirusResult = GenerateAntiVirusResult(submissionId, fileId, blobName);

        var registrationValidationErrorQueryEvents = new List<AbstractValidationError>
        {
            new RegistrationValidationError()
            {
                ValidationEventId = validationEventId,
                BlobName = blobName,
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "840",
                        ColumnIndex = 3,
                        ColumnName = "trading_name"
                    },
                },
                OrganisationId = "876",
                SubsidiaryId = "8865",
            },
            new RegistrationValidationError()
            {
                ValidationEventId = validationEventId,
                BlobName = blobName,
                ColumnErrors = new List<ColumnValidationError>
                {
                    new()
                    {
                        ErrorCode = "840",
                        ColumnIndex = 3,
                        ColumnName = "trading_name"
                    },
                },
                OrganisationId = "876",
                SubsidiaryId = "9000",
            }
        };

        var request = new RegistrationValidationErrorQuery(submissionId, organisationId);

        _validationEventHelperMock.
            Setup(x => x.GetLatestAntivirusResult(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResult);

        _validationEventErrorQueryRepositoryMock
            .Setup(x => x.GetAll(It.Is<Expression<Func<AbstractValidationError, bool>>>(y => y.Compile().Invoke(new RegistrationValidationError() { BlobName = blobName }))))
            .Returns(registrationValidationErrorQueryEvents.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task Handle_ReturnsValidErrorDetails_WhenValidValidationErrorEventExists()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var blobName = Guid.NewGuid().ToString();

        var antivirusResult = GenerateAntiVirusResult(submissionId, fileId, blobName);

        _validationEventHelperMock.
            Setup(x => x.GetLatestAntivirusResult(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResult);

        _validationEventErrorQueryRepositoryMock
            .Setup(x => x.GetAll(It.Is<Expression<Func<AbstractValidationError, bool>>>(y => y.Compile().Invoke(new RegistrationValidationError() { BlobName = blobName }))))
            .Returns(GenerateRegistrationValidationErrorQueryEventMock);

        var request = new RegistrationValidationErrorQuery(submissionId, organisationId);

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().ColumnErrors.Count.Should().Be(2);
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().ColumnErrors[0].ErrorCode.Should().Be("840");
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().ColumnErrors[0].ColumnIndex.Should().Be(3);
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().ColumnErrors[0].ColumnName.Should().Be("trading_name");
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().OrganisationId.Should().Be("876");
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().SubsidiaryId.Should().Be("8865");
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().RowNumber.Should().Be(4);
        result.Value.As<List<AbstractValidationIssueGetResponse>>().FirstOrDefault()
            .As<RegistrationValidationIssueGetResponse>().ErrorCodes.Should().BeEmpty();
    }

    private IQueryable<AbstractValidationError> GenerateRegistrationValidationErrorQueryEventMock()
    {
        return new List<AbstractValidationError>
        {
            new RegistrationValidationError
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
                OrganisationId = "876",
                SubsidiaryId = "8865",
                RowNumber = 4,
                ErrorCodes = { }
            },
        }.BuildMock();
    }

    private AntivirusResultEvent GenerateAntiVirusResult(Guid submissionId, Guid fileId, string blobName)
    {
        return new AntivirusResultEvent()
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            FileId = fileId,
            AntivirusScanResult = AntivirusScanResult.Success,
            BlobName = blobName
        };
    }
}