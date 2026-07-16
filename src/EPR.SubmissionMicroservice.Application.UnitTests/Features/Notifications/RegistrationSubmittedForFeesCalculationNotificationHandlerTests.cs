namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Notifications;

using Application.Messaging.Publishing.RegistrationSubmittedForFeesCalculation;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Moq;

[TestClass]
public class RegistrationSubmittedForFeesCalculationNotificationHandlerTests
{
    private Mock<ILogger<RegistrationSubmittedForFeesCalculationNotificationHandler>> _loggerMock = null!;
    private Mock<IAzureClientFactory<ServiceBusSender>> _senderFactoryMock = null!;
    private Mock<ServiceBusSender> _senderMock = null!;

    [TestInitialize]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<RegistrationSubmittedForFeesCalculationNotificationHandler>>();
        _senderMock = new Mock<ServiceBusSender>();
        _senderFactoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
        _senderFactoryMock
            .Setup(f => f.CreateClient(nameof(RegistrationSubmittedForFeesCalculationNotification)))
            .Returns(_senderMock.Object);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionPeriodIdIsNull_ThrowsAndDoesNotPublish()
    {
        var notification = new RegistrationSubmittedForFeesCalculationNotification(
            SubmissionId: Guid.NewGuid(),
            RegistrationBlobName: "blob",
            ComplianceSchemeId: null,
            SubmissionDate: DateTime.UtcNow,
            SubmissionPeriodId: null);

        var handler = new RegistrationSubmittedForFeesCalculationNotificationHandler(_loggerMock.Object, _senderFactoryMock.Object);

        Func<Task> act = () => handler.Handle(notification, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SubmissionPeriodId*");

        _senderMock.Verify(
            s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_WhenSubmissionPeriodIdIsSet_Publishes()
    {
        var notification = new RegistrationSubmittedForFeesCalculationNotification(
            SubmissionId: Guid.NewGuid(),
            RegistrationBlobName: "blob",
            ComplianceSchemeId: Guid.NewGuid(),
            SubmissionDate: DateTime.UtcNow,
            SubmissionPeriodId: 1);

        var handler = new RegistrationSubmittedForFeesCalculationNotificationHandler(_loggerMock.Object, _senderFactoryMock.Object);

        await handler.Handle(notification, CancellationToken.None);

        _senderMock.Verify(
            s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
