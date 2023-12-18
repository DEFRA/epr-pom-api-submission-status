namespace EPR.SubmissionMicroservice.Data.UnitTests.Constants;

using Data.Constants;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ValidationConstantsTests
{
    [TestMethod]
    public void TestMinSubmissionPeriodLength_ShouldBeSetTo4()
    {
        // Assert
        ValidationConstants.MinSubmissionPeriodLength.Should().Be(4);
    }

    [TestMethod]
    public void TestMaxFileNameLength_ShouldBeSetTo100()
    {
        // Assert
        ValidationConstants.MaxFileNameLength.Should().Be(100);
    }
}