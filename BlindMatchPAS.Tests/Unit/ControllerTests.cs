using BlindMatchPAS.Controllers;
using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace BlindMatchPAS.Tests.Unit
{
    /// <summary>
    /// Unit tests for controllers using Moq to mock services and UserManager.
    /// Verifies correct redirects, TempData, and service delegation.
    /// </summary>
    public class SupervisorControllerTests : IDisposable
    {
        private readonly Mock<IBlindMatchService> _mockService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly ApplicationDbContext _db;
        private readonly SupervisorController _controller;
        private const string TestSupervisorId = "sv-test-001";

        public SupervisorControllerTests()
        {
            // Setup InMemory DB
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);

            // Mock IBlindMatchService
            _mockService = new Mock<IBlindMatchService>();

            // Mock UserManager
            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
                           .Returns(TestSupervisorId);

            // Build controller with mocked dependencies
            _controller = new SupervisorController(_mockService.Object, _db, _mockUserManager.Object);

            // Setup HttpContext and TempData
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task ExpressInterest_WhenSuccessful_RedirectsToDashboard_WithSuccessMessage()
        {
            // Arrange
            _mockService.Setup(s => s.ExpressInterestAsync(TestSupervisorId, 1))
                       .ReturnsAsync(true);

            // Act
            var result = await _controller.ExpressInterest(1);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be("Dashboard");
            _controller.TempData["Success"].Should().NotBeNull();
        }

        [Fact]
        public async Task ExpressInterest_WhenFails_RedirectsToDashboard_WithErrorMessage()
        {
            // Arrange
            _mockService.Setup(s => s.ExpressInterestAsync(TestSupervisorId, 1))
                       .ReturnsAsync(false);

            // Act
            var result = await _controller.ExpressInterest(1);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be("Dashboard");
            _controller.TempData["Error"].Should().NotBeNull();
        }

        [Fact]
        public async Task ConfirmMatch_WhenSuccessful_RedirectsToDashboard_WithSuccessMessage()
        {
            // Arrange
            _mockService.Setup(s => s.ConfirmMatchAsync(TestSupervisorId, 5))
                       .ReturnsAsync(true);

            // Act
            var result = await _controller.ConfirmMatch(5);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be("Dashboard");
            _controller.TempData["Success"].Should().NotBeNull();
        }

        [Fact]
        public async Task ConfirmMatch_CallsServiceWithCorrectParameters()
        {
            // Arrange
            _mockService.Setup(s => s.ConfirmMatchAsync(It.IsAny<string>(), It.IsAny<int>()))
                       .ReturnsAsync(true);

            // Act
            await _controller.ConfirmMatch(42);

            // Assert: verify service was called with correct supervisor ID and match ID
            _mockService.Verify(s => s.ConfirmMatchAsync(TestSupervisorId, 42), Times.Once);
        }

        public void Dispose() => _db.Dispose();
    }

    public class StudentControllerTests : IDisposable
    {
        private readonly Mock<IBlindMatchService> _mockService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly ApplicationDbContext _db;
        private readonly StudentController _controller;
        private const string TestStudentId = "st-test-001";

        public StudentControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);

            _mockService = new Mock<IBlindMatchService>();

            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
                           .Returns(TestStudentId);

            _controller = new StudentController(_mockService.Object, _db, _mockUserManager.Object);
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task Withdraw_WhenSuccessful_SetsSuccessTempData()
        {
            // Arrange
            _mockService.Setup(s => s.WithdrawProposalAsync(TestStudentId, 1))
                       .ReturnsAsync(true);

            // Act
            var result = await _controller.Withdraw(1);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be("Dashboard");
            _controller.TempData["Success"].Should().NotBeNull();
        }

        [Fact]
        public async Task Withdraw_WhenFails_SetsErrorTempData()
        {
            // Arrange
            _mockService.Setup(s => s.WithdrawProposalAsync(TestStudentId, 1))
                       .ReturnsAsync(false);

            // Act
            var result = await _controller.Withdraw(1);

            // Assert
            _controller.TempData["Error"].Should().NotBeNull();
        }

        [Fact]
        public async Task Submit_ValidModel_CreatesProposalAndRedirects()
        {
            // Arrange: seed a research area
            _db.ResearchAreas.Add(new ResearchArea { Id = 1, Name = "AI", IsActive = true });
            await _db.SaveChangesAsync();

            // Setup controller user
            var user = new ApplicationUser { Id = TestStudentId, FullName = "Test", UserName = "test@test.com", Email = "test@test.com", Role = "Student" };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var model = new SubmitProposalViewModel
            {
                Title = "My AI Project",
                Abstract = "This is a detailed abstract about AI research that meets the minimum length requirement.",
                TechnicalStack = "Python, TensorFlow",
                ResearchAreaId = 1
            };

            // Act
            var result = await _controller.Submit(model);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be("Dashboard");

            var saved = await _db.ProjectProposals.FirstOrDefaultAsync(p => p.StudentId == TestStudentId);
            saved.Should().NotBeNull();
            saved!.Title.Should().Be("My AI Project");
            saved.Status.Should().Be(ProjectStatus.Pending);
        }

        public void Dispose() => _db.Dispose();
    }
}
