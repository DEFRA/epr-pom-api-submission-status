namespace EPR.SubmissionMicroservice.Application.UnitTests.Features.Queries.SubmissionGet;

using Application.Features.Queries.Common;
using Application.Features.Queries.Helpers.Interfaces;
using Application.Features.Queries.SubmissionGet;
using AutoMapper;
using Data.Constants;
using Data.Entities.Submission;
using Data.Enums;
using Data.Repositories.Queries.Interfaces;
using FluentAssertions;
using Moq;
using TestSupport.Helpers;

[TestClass]
public class SubmissionGetQueryHandlerTests
{
    private const string SubmissionPeriod = "Jan to Jun 23";
    private readonly Guid _submissionId = Guid.NewGuid();
    private readonly Guid _organisationId = Guid.NewGuid();
    private readonly IMapper _mapper = AutoMapperHelpers.GetMapper();
    private Mock<IQueryRepository<Submission>> _submissionQueryRepositoryMock;
    private Mock<IPomSubmissionEventHelper> _pomSubmissionEventHelperMock;
    private Mock<IRegistrationSubmissionEventHelper> _registrationSubmissionEventHelperMock;
    private Mock<ISubsidiarySubmissionEventHelper> _subsidiarySubmissionEventHelperMock;
    private Mock<ICompaniesHouseSubmissionEventHelper> _companiesHouseSubmissionEventHelperMock;
    private Mock<IAccreditationSubmissionEventHelper> _accreditationSubmissionEventHelperMock;
    private SubmissionGetQueryHandler _systemUnderTest;

    [TestInitialize]
    public void SetUp()
    {
        _pomSubmissionEventHelperMock = new Mock<IPomSubmissionEventHelper>();
        _registrationSubmissionEventHelperMock = new Mock<IRegistrationSubmissionEventHelper>();
        _subsidiarySubmissionEventHelperMock = new Mock<ISubsidiarySubmissionEventHelper>();
        _companiesHouseSubmissionEventHelperMock = new Mock<ICompaniesHouseSubmissionEventHelper>();
        _submissionQueryRepositoryMock = new Mock<IQueryRepository<Submission>>();
        _accreditationSubmissionEventHelperMock = new Mock<IAccreditationSubmissionEventHelper>();

        _systemUnderTest = new SubmissionGetQueryHandler(
            _submissionQueryRepositoryMock.Object,
            _pomSubmissionEventHelperMock.Object,
            _registrationSubmissionEventHelperMock.Object,
            _subsidiarySubmissionEventHelperMock.Object,
            _companiesHouseSubmissionEventHelperMock.Object,
            _accreditationSubmissionEventHelperMock.Object,
            _mapper);
    }

    [TestMethod]
    public async Task Handle_ReturnsErrorNotFound_WhenSubmissionDoesNotExist()
    {
        // Arrange
        var submissionGetQuery = new SubmissionGetQuery(_submissionId, _organisationId);

        // Act
        var result = await _systemUnderTest.Handle(submissionGetQuery, CancellationToken.None);

        // Assert
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Code.Should().Be("General.NotFound");
    }

    [TestMethod]
    public async Task Handle_ReturnsErrorOrganisationUnauthorized_WhenSubmissionDoesNotBelongToOrganisation()
    {
        // Arrange
        var submissionGetQuery = new SubmissionGetQuery(_submissionId, _organisationId);

        var pomSubmission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = Guid.NewGuid(),
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = SubmissionPeriod,
            IsSubmitted = false
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(pomSubmission);

        // Act
        var result = await _systemUnderTest.Handle(submissionGetQuery, CancellationToken.None);

        // Assert
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0];
        error.NumericType.Should().Be(CustomErrorType.Unauthorized);
        error.Code.Should().Be(CustomErrorCode.OrganisationUnauthorized);
        error.Description.Should().Be("Your organisation is not authorized to access the submission.");
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponse_ForPomSubmission()
    {
        // Arrange
        var submissionGetQuery = new SubmissionGetQuery(_submissionId, _organisationId);

        var pomSubmission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = _organisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = SubmissionPeriod,
            IsSubmitted = false
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(pomSubmission);

        // Act
        var result = await _systemUnderTest.Handle(submissionGetQuery, CancellationToken.None);

        // Assert
        var expectedResult = new PomSubmissionGetResponse
        {
            Id = pomSubmission.Id,
            SubmissionType = SubmissionType.Producer,
            OrganisationId = _organisationId,
            SubmissionPeriod = SubmissionPeriod
        };
        result.Value.Should().BeEquivalentTo(expectedResult);
        _pomSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<PomSubmissionGetResponse>(), false, CancellationToken.None), Times.Once);
        _registrationSubmissionEventHelperMock.Verify(x => x.SetValidationEvents(It.IsAny<RegistrationSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponse_ForSubmittedPomSubmission()
    {
        // Arrange
        var submissionGetQuery = new SubmissionGetQuery(_submissionId, _organisationId);

        var pomSubmission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = _organisationId,
            SubmissionType = SubmissionType.Producer,
            SubmissionPeriod = SubmissionPeriod,
            IsSubmitted = true
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(pomSubmission);

        // Act
        var result = await _systemUnderTest.Handle(submissionGetQuery, CancellationToken.None);

        // Assert
        var expectedResult = new PomSubmissionGetResponse
        {
            Id = pomSubmission.Id,
            SubmissionType = SubmissionType.Producer,
            OrganisationId = _organisationId,
            SubmissionPeriod = SubmissionPeriod,
            IsSubmitted = true
        };
        result.Value.Should().BeEquivalentTo(expectedResult);
        _pomSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<PomSubmissionGetResponse>(), true, CancellationToken.None), Times.Once);
        _registrationSubmissionEventHelperMock.Verify(x => x.SetValidationEvents(It.IsAny<RegistrationSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponse_ForRegistrationSubmission()
    {
        // Arrange
        var submissionGetQuery = new SubmissionGetQuery(_submissionId, _organisationId);

        var registrationSubmission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = _organisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = SubmissionPeriod,
            IsSubmitted = false
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(registrationSubmission);

        // Act
        var result = await _systemUnderTest.Handle(submissionGetQuery, CancellationToken.None);

        // Assert
        var expectedResult = new RegistrationSubmissionGetResponse
        {
            Id = registrationSubmission.Id,
            SubmissionType = SubmissionType.Registration,
            OrganisationId = _organisationId,
            SubmissionPeriod = SubmissionPeriod
        };
        result.Value.Should().BeEquivalentTo(expectedResult);
        _registrationSubmissionEventHelperMock.Verify(x => x.SetValidationEvents(It.IsAny<RegistrationSubmissionGetResponse>(), false, CancellationToken.None), Times.Once);
        _pomSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<PomSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponse_ForSubmittedRegistrationSubmission()
    {
        // Arrange
        var submissionGetQuery = new SubmissionGetQuery(_submissionId, _organisationId);

        var registrationSubmission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = _organisationId,
            SubmissionType = SubmissionType.Registration,
            SubmissionPeriod = SubmissionPeriod,
            IsSubmitted = true
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(registrationSubmission);

        // Act
        var result = await _systemUnderTest.Handle(submissionGetQuery, CancellationToken.None);

        // Assert
        var expectedResult = new RegistrationSubmissionGetResponse
        {
            Id = registrationSubmission.Id,
            SubmissionType = SubmissionType.Registration,
            OrganisationId = _organisationId,
            SubmissionPeriod = SubmissionPeriod,
            IsSubmitted = true
        };
        result.Value.Should().BeEquivalentTo(expectedResult);
        _registrationSubmissionEventHelperMock.Verify(x => x.SetValidationEvents(It.IsAny<RegistrationSubmissionGetResponse>(), true, CancellationToken.None), Times.Once);
        _pomSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<PomSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponse_ForSubsidiarySubmission()
    {
        // Arrange
        var submissionGetQuery = new SubmissionGetQuery(_submissionId, _organisationId);

        var subsidiarySubmission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = _organisationId,
            SubmissionType = SubmissionType.Subsidiary,
            IsSubmitted = false
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(subsidiarySubmission);

        // Act
        var result = await _systemUnderTest.Handle(submissionGetQuery, CancellationToken.None);

        // Assert
        var expectedResult = new SubsidiarySubmissionGetResponse
        {
            Id = subsidiarySubmission.Id,
            SubmissionType = SubmissionType.Subsidiary,
            OrganisationId = _organisationId
        };
        result.Value.Should().BeEquivalentTo(expectedResult);
        _subsidiarySubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<SubsidiarySubmissionGetResponse>(), false, CancellationToken.None), Times.Once);
        _pomSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<PomSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
        _registrationSubmissionEventHelperMock.Verify(x => x.SetValidationEvents(It.IsAny<RegistrationSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponse_ForSubmittedSubsidiarySubmission()
    {
        // Arrange
        var submissionGetQuery = new SubmissionGetQuery(_submissionId, _organisationId);

        var subsidiarySubmission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = _organisationId,
            SubmissionType = SubmissionType.Subsidiary,
            SubmissionPeriod = null,
            IsSubmitted = true
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(subsidiarySubmission);

        // Act
        var result = await _systemUnderTest.Handle(submissionGetQuery, CancellationToken.None);

        // Assert
        var expectedResult = new SubsidiarySubmissionGetResponse
        {
            Id = subsidiarySubmission.Id,
            SubmissionType = SubmissionType.Subsidiary,
            OrganisationId = _organisationId,
            IsSubmitted = true
        };
        result.Value.Should().BeEquivalentTo(expectedResult);
        _subsidiarySubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<SubsidiarySubmissionGetResponse>(), true, CancellationToken.None), Times.Once);
        _pomSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<PomSubmissionGetResponse>(), true, CancellationToken.None), Times.Never);
        _registrationSubmissionEventHelperMock.Verify(x => x.SetValidationEvents(It.IsAny<RegistrationSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponse_ForCompaniesHouseSubmission()
    {
        // Arrange
        var submissionGetQuery = new SubmissionGetQuery(_submissionId, _organisationId);

        var companiesHouseSubmission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = _organisationId,
            SubmissionType = SubmissionType.CompaniesHouse,
            IsSubmitted = false
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(companiesHouseSubmission);

        // Act
        var result = await _systemUnderTest.Handle(submissionGetQuery, CancellationToken.None);

        // Assert
        var expectedResult = new CompaniesHouseSubmissionGetResponse
        {
            Id = companiesHouseSubmission.Id,
            SubmissionType = SubmissionType.CompaniesHouse,
            OrganisationId = _organisationId
        };
        result.Value.Should().BeEquivalentTo(expectedResult);
        _companiesHouseSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<CompaniesHouseSubmissionGetResponse>(), CancellationToken.None), Times.Once);
        _pomSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<PomSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
        _registrationSubmissionEventHelperMock.Verify(x => x.SetValidationEvents(It.IsAny<RegistrationSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponse_ForSubmittedCompaniesHouseSubmission()
    {
        // Arrange
        var submissionGetQuery = new SubmissionGetQuery(_submissionId, _organisationId);

        var companiesHouseSubmission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = _organisationId,
            SubmissionType = SubmissionType.CompaniesHouse,
            SubmissionPeriod = null,
            IsSubmitted = true
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(companiesHouseSubmission);

        // Act
        var result = await _systemUnderTest.Handle(submissionGetQuery, CancellationToken.None);

        // Assert
        var expectedResult = new CompaniesHouseSubmissionGetResponse
        {
            Id = companiesHouseSubmission.Id,
            SubmissionType = SubmissionType.CompaniesHouse,
            OrganisationId = _organisationId,
            IsSubmitted = true
        };
        result.Value.Should().BeEquivalentTo(expectedResult);
        _companiesHouseSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<CompaniesHouseSubmissionGetResponse>(), CancellationToken.None), Times.Once);
        _pomSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<PomSubmissionGetResponse>(), true, CancellationToken.None), Times.Never);
        _registrationSubmissionEventHelperMock.Verify(x => x.SetValidationEvents(It.IsAny<RegistrationSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ReturnsExpectedGetResponse_ForAccreditationSubmission()
    {
        // Arrange
        var submissionGetQuery = new SubmissionGetQuery(_submissionId, _organisationId);

        var accreditationSubmission = new Submission
        {
            Id = Guid.NewGuid(),
            OrganisationId = _organisationId,
            SubmissionType = SubmissionType.Accreditation
        };

        _submissionQueryRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(accreditationSubmission);

        // Act
        var result = await _systemUnderTest.Handle(submissionGetQuery, CancellationToken.None);

        // Assert
        var expectedResult = new AccreditationSubmissionGetResponse
        {
            Id = accreditationSubmission.Id,
            SubmissionType = SubmissionType.Accreditation,
            OrganisationId = _organisationId
        };
        result.Value.Should().BeEquivalentTo(expectedResult);
        _accreditationSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<AccreditationSubmissionGetResponse>(), false, CancellationToken.None), Times.Once);
        _registrationSubmissionEventHelperMock.Verify(x => x.SetValidationEvents(It.IsAny<RegistrationSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
        _pomSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<PomSubmissionGetResponse>(), It.IsAny<bool>(), CancellationToken.None), Times.Never);
        _subsidiarySubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<SubsidiarySubmissionGetResponse>(), true, CancellationToken.None), Times.Never);
        _companiesHouseSubmissionEventHelperMock.Verify(x => x.SetValidationEventsAsync(It.IsAny<CompaniesHouseSubmissionGetResponse>(), CancellationToken.None), Times.Never);
    }
}