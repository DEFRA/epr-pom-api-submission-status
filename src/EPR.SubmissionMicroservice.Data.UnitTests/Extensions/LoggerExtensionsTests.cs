namespace EPR.SubmissionMicroservice.Data.UnitTests.Extensions;

using Data.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

public class LoggerExtensionsTests
{
    private Mock<ILogger<LoggerExtensionsTests>>? _loggerMock;

    [TestInitialize]
    public void Initialize()
    {
        _loggerMock = new Mock<ILogger<LoggerExtensionsTests>>();
    }

    [TestMethod]
    public void LogEnter_LogsEnteringCallingMethodName_WhenCalled()
    {
        // Act
        _loggerMock.Object.LogEnter();

        // Assert
        _loggerMock.VerifyLog(x => x.LogInformation("Entering LogEnter_LogsEnteringCallingMethodName_WhenCalled"), Times.Once);
    }

    [TestMethod]
    public void LogExit_LogsExitingCallingMethodName_WhenCalled()
    {
        // Act
        _loggerMock.Object.LogExit();

        // Assert
        _loggerMock.VerifyLog(x => x.LogInformation("Exiting LogExit_LogsExitingCallingMethodName_WhenCalled"), Times.Once);
    }
}