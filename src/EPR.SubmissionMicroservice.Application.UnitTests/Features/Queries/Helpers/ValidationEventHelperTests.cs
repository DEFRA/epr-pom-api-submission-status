using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers;
using EPR.SubmissionMicroservice.Application.Features.Queries.Helpers.Interfaces;
using EPR.SubmissionMicroservice.Data.Entities.AntivirusEvents;
using EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent;
using EPR.SubmissionMicroservice.Data.Enums;
using EPR.SubmissionMicroservice.Data.Repositories.Queries.Interfaces;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.Helpers;

[TestClass]
public class ValidationEventHelperTests
{
    private Mock<IQueryRepository<AbstractSubmissionEvent>> _submissionEventQueryRepositoryMock;
    private IValidationEventHelper _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _submissionEventQueryRepositoryMock = new Mock<IQueryRepository<AbstractSubmissionEvent>>();

        _systemUnderTest = new ValidationEventHelper(_submissionEventQueryRepositoryMock.Object);
    }

    [TestMethod]
    public async Task GetLatestAntivirusResult_ReturnsNull_WhenNoEventsExist()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        var antivirusCheckQueryEvents = new List<AbstractSubmissionEvent>();

        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.Is<Expression<Func<AbstractSubmissionEvent, bool>>>(y => y.Compile().Invoke(new AntivirusCheckEvent { SubmissionId = submissionId }))))
            .Returns(antivirusCheckQueryEvents.BuildMock);

        // Act
        var result = await _systemUnderTest.GetLatestAntivirusResult(submissionId, It.IsAny<CancellationToken>());

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetLatestAntivirusResult_ReturnsLatestAntivirusResult_WhenEventExists()
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var blobName = "some-file.csv";

        var antivirusCheckQueryEvents = new List<AbstractSubmissionEvent>
        {
            new AntivirusCheckEvent
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                FileId = fileId,
                FileType = FileType.Pom,
                FileName = blobName
            }
        };

        var antivirusResultQueryEvents = new List<AbstractSubmissionEvent>
        {
            new AntivirusResultEvent
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                FileId = fileId,
                AntivirusScanResult = AntivirusScanResult.Success,
                BlobName = blobName
            }
        };

        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.Is<Expression<Func<AbstractSubmissionEvent, bool>>>(y => y.Compile().Invoke(new AntivirusCheckEvent { SubmissionId = submissionId }))))
            .Returns(antivirusCheckQueryEvents.BuildMock);

        _submissionEventQueryRepositoryMock
            .Setup(x => x.GetAll(It.Is<Expression<Func<AbstractSubmissionEvent, bool>>>(y => y.Compile().Invoke(new AntivirusResultEvent { SubmissionId = submissionId }))))
            .Returns(antivirusResultQueryEvents.BuildMock);

        // Act
        var result = await _systemUnderTest.GetLatestAntivirusResult(submissionId, It.IsAny<CancellationToken>());

        // Assert
        result.Should().BeEquivalentTo(antivirusResultQueryEvents.First());
    }
}