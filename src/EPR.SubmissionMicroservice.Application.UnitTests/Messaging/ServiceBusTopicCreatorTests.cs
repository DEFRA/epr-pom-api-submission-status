using Azure.Messaging.ServiceBus.Administration;
using EPR.SubmissionMicroservice.Application.Messaging.Publishing;
using EPR.SubmissionMicroservice.Application.Options;
using Microsoft.Extensions.Options;

namespace EPR.SubmissionMicroservice.Application.UnitTests.Messaging;

[TestClass]
public class ServiceBusTopicCreatorTests
{
    private ServiceBusTopicCreator _systemUnderTest;
    private Mock<ILogger<ServiceBusTopicCreator>> _loggerMock;
    private Mock<ServiceBusAdministrationClient> _adminClientMock;
    private Mock<IOptions<ServiceBusOptions>> _serviceBusOptionsMock;
    private ServiceBusOptions _serviceBusOptions;
    private const string FeesTopicName = "test-topic1";
    private const string ApprovalTopicName = "test-topic2";

    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<ServiceBusTopicCreator>>();
        _adminClientMock = new Mock<ServiceBusAdministrationClient>();
        _serviceBusOptions = new ServiceBusOptions
        {
            RegistrationSubmittedForFeesCalculationTopicName = FeesTopicName,
            RegistrationSubmittedForRegulatorApprovalTopicName = ApprovalTopicName
        };
           
        _serviceBusOptionsMock = new Mock<IOptions<ServiceBusOptions>>();
        _serviceBusOptionsMock.Setup(x => x.Value).Returns(_serviceBusOptions);

        _systemUnderTest = new ServiceBusTopicCreator(
            _loggerMock.Object,
            _adminClientMock.Object,
            _serviceBusOptionsMock.Object);
    }

    [TestMethod]
    public async Task ConfigureTopics_WhenTopicsExists_ShouldNotCreateTopic()
    {
        // Arrange
        var topicExistsResult = new Mock<Azure.Response<bool>>();
        topicExistsResult.Setup(x => x.Value).Returns(true);
        topicExistsResult.Setup(x => x.HasValue).Returns(true);
        
        _adminClientMock
            .Setup(x => x.TopicExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(topicExistsResult.Object);

        // Act
        await _systemUnderTest.ConfigureTopics();

        // Assert
        _adminClientMock.Verify(x => x.CreateTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _adminClientMock.Verify(x => x.TopicExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task ConfigureTopics_WhenTopicDoesNotExist_ShouldCreateTopic()
    {
        // Arrange
        var feesTopicExistsResult = new Mock<Azure.Response<bool>>();
        feesTopicExistsResult.Setup(x => x.Value).Returns(false);
        feesTopicExistsResult.Setup(x => x.HasValue).Returns(true);
        var approvalTopicExistsResult = new Mock<Azure.Response<bool>>();
        approvalTopicExistsResult.Setup(x => x.Value).Returns(true);
        approvalTopicExistsResult.Setup(x => x.HasValue).Returns(true);
        
        _adminClientMock
            .Setup(x => x.TopicExistsAsync(FeesTopicName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feesTopicExistsResult.Object);
        _adminClientMock
            .Setup(x => x.TopicExistsAsync(ApprovalTopicName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvalTopicExistsResult.Object);

        var createTopicResult = new Mock<Azure.Response<TopicProperties>>();
        _adminClientMock
            .Setup(x => x.CreateTopicAsync(FeesTopicName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createTopicResult.Object);

        // Act
        await _systemUnderTest.ConfigureTopics();

        // Assert
        _adminClientMock.Verify(x => x.CreateTopicAsync(FeesTopicName, It.IsAny<CancellationToken>()), Times.Once);
        _adminClientMock.Verify(x => x.CreateTopicAsync(ApprovalTopicName, It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task ConfigureTopics_WhenTopicExistsAsyncReturnsNullValue_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var topicExistsResult = new Mock<Azure.Response<bool>>();
        topicExistsResult.Setup(x => x.HasValue).Returns(false);
        _adminClientMock
            .Setup(x => x.TopicExistsAsync(FeesTopicName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topicExistsResult.Object);

        // Act
         await _systemUnderTest.ConfigureTopics();

        // Assert - exception is expected
    }

    [TestMethod]
    public async Task ConfigureTopics_WhenTopicExists_ShouldLogInformationMessages()
    {
        // Arrange
        var topicExistsResult = new Mock<Azure.Response<bool>>();
        topicExistsResult.Setup(x => x.Value).Returns(true);
        topicExistsResult.Setup(x => x.HasValue).Returns(true);
        
        _adminClientMock
            .Setup(x => x.TopicExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(topicExistsResult.Object);

        // Act
        await _systemUnderTest.ConfigureTopics();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task ConfigureTopics_WhenTopicDoesNotExist_ShouldLogCreationMessages()
    {
        // Arrange
        var topicExistsResult = new Mock<Azure.Response<bool>>();
        topicExistsResult.Setup(x => x.Value).Returns(false);
        topicExistsResult.Setup(x => x.HasValue).Returns(true);
        
        _adminClientMock
            .Setup(x => x.TopicExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(topicExistsResult.Object);

        var createTopicResult = new Mock<Azure.Response<TopicProperties>>();
        _adminClientMock
            .Setup(x => x.CreateTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createTopicResult.Object);

        // Act
        await _systemUnderTest.ConfigureTopics();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Creating topic")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(2));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("successfully created")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task ConfigureTopics_ShouldUseTopicNameFromOptions()
    {
        // Arrange
        var topicExistsResult = new Mock<Azure.Response<bool>>();
        topicExistsResult.Setup(x => x.Value).Returns(true);
        topicExistsResult.Setup(x => x.HasValue).Returns(true);
        
        _adminClientMock
            .Setup(x => x.TopicExistsAsync(FeesTopicName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topicExistsResult.Object);
        _adminClientMock
            .Setup(x => x.TopicExistsAsync(ApprovalTopicName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topicExistsResult.Object);

        // Act
        await _systemUnderTest.ConfigureTopics();

        // Assert
        _adminClientMock.Verify(
            x => x.TopicExistsAsync(FeesTopicName, It.IsAny<CancellationToken>()),
            Times.Once);
        _adminClientMock.Verify(
            x => x.TopicExistsAsync(ApprovalTopicName, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
