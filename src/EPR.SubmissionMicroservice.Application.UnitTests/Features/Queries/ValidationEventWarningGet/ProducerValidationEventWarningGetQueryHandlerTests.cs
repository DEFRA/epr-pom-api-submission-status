using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Application.Features.Queries.ValidationEventWarningGet;
using EPR.SubmissionMicroservice.Application.Options;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.ValidationEventWarning;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.ValidationEventWarningGet;

[TestClass]
public class ProducerValidationEventWarningGetQueryHandlerTests
{
    private ValidationEventWarningGetQueryHandler _systemUnderTest;
    private Mock<IQueryRepository<AbstractValidationWarning>> _validationEventWarningQueryRepositoryMock;
    private Mock<IValidationEventHelper> _validationEventHelperMock;

    [TestInitialize]
    public void TestInitialize()
    {
        var validationOptions = new ValidationOptions { MaxIssuesToProcess = 1 };
        _validationEventWarningQueryRepositoryMock = new Mock<IQueryRepository<AbstractValidationWarning>>();
        _validationEventHelperMock = new Mock<IValidationEventHelper>();
        _systemUnderTest = new ValidationEventWarningGetQueryHandler(
            _validationEventWarningQueryRepositoryMock.Object,
            _validationEventHelperMock.Object,
            AutoMapperHelpers.GetMapper(),
            Microsoft.Extensions.Options.Options.Create(validationOptions));
    }

    [TestMethod]
    public async Task Handle_ReturnsNoErrors_WhenNoEventsExist()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        var request = new ValidationEventWarningGetQuery(submissionId);

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
            new ProducerValidationWarning()
            {
                ValidationEventId = producerValidationEventId,
                BlobName = blobName
            }
        };

        var request = new ValidationEventWarningGetQuery(submissionId);

        _validationEventHelperMock.
            Setup(x => x.GetLatestAntivirusResult(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResult);

        _validationEventWarningQueryRepositoryMock
            .Setup(x => x.GetAll(It.Is<Expression<Func<AbstractValidationWarning, bool>>>(y => y.Compile().Invoke(new ProducerValidationWarning { BlobName = blobName }))))
            .Returns(producerValidationWarningQueryEvents.BuildMock);

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
            new ProducerValidationWarning()
            {
                ValidationEventId = producerValidationEventId,
                BlobName = blobName
            },
            new ProducerValidationWarning()
            {
                ValidationEventId = producerValidationEventId,
                BlobName = blobName
            }
        };

        var request = new ValidationEventWarningGetQuery(submissionId);

        _validationEventHelperMock.
            Setup(x => x.GetLatestAntivirusResult(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(antivirusResult);

        _validationEventWarningQueryRepositoryMock
            .Setup(x => x.GetAll(It.Is<Expression<Func<AbstractValidationWarning, bool>>>(y => y.Compile().Invoke(new ProducerValidationWarning() { BlobName = blobName }))))
            .Returns(producerValidationErrorQueryEvents.BuildMock);

        // Act
        var result = await _systemUnderTest.Handle(request, CancellationToken.None);

        // Assert
        result.Value.Should().HaveCount(1);
    }
}