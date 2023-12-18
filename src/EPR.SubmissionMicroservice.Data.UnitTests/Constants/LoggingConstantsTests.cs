namespace EPR.SubmissionMicroservice.Data.UnitTests.Constants;

using Data.Constants;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class LoggingConstantsTests
{
    [TestMethod]
    public void TestLongRunningRequestTime_ShouldBeSetTo500()
    {
        // Assert
        LoggingConstants.LongRunningRequestTime.Should().Be(500);
    }
}