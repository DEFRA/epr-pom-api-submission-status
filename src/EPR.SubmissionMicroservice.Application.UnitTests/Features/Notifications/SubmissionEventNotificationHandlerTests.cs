namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Notifications;

using Application.Features.Notifications.SubmissionEventNotification;
using Microsoft.Extensions.Logging;
using Moq;

[TestClass]
public class SubmissionEventNotificationHandlerTests
{
    private Mock<ILogger<SubmissionEventNotificationHandler>> _loggerMock;

    [TestInitialize]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<SubmissionEventNotificationHandler>>();
    }

    [TestMethod]
    public async Task TestHandle_WhenCalled_ShouldLogCriticalMessage()
    {
        // Arrange
        var notification = new SubmissionEventNotification(Guid.NewGuid());
        var cancellationToken = CancellationToken.None;

        var handler = new SubmissionEventNotificationHandler(_loggerMock.Object);

        // Act
        await handler.Handle(notification, cancellationToken);

        // Assert
        _loggerMock.VerifyLog(
            x => x.LogCritical("SubmissionEventNotificationHandler: {Id}", notification.Id));
    }
}
