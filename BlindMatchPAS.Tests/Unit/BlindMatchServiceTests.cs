using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlindMatchPAS.Tests.Unit
{
    /// <summary>
    /// Unit tests for the core BlindMatchService business logic.
    /// Uses EF Core InMemory database to isolate from SQL Server.
    /// </summary>
    public class BlindMatchServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly BlindMatchService _service;

        // ── Test fixtures ──────────────────────────────────────────────────────
        private const string StudentId = "student-001";
        private const string SupervisorId = "supervisor-001";
        private const string Supervisor2Id = "supervisor-002";

        public BlindMatchServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
            _service = new BlindMatchService(_db);

            SeedTestData();
        }

        private void SeedTestData()
        {
            _db.Users.AddRange(
                new ApplicationUser { Id = StudentId, FullName = "Alice Student", Email = "alice@test.com", UserName = "alice@test.com", Role = "Student" },
                new ApplicationUser { Id = SupervisorId, FullName = "Bob Supervisor", Email = "bob@test.com", UserName = "bob@test.com", Role = "Supervisor" },
                new ApplicationUser { Id = Supervisor2Id, FullName = "Carol Supervisor", Email = "carol@test.com", UserName = "carol@test.com", Role = "Supervisor" }
            );

            _db.ResearchAreas.Add(new ResearchArea { Id = 1, Name = "AI", IsActive = true });

            _db.ProjectProposals.Add(new ProjectProposal
            {
                Id = 1,
                Title = "Smart AI Classifier",
                Abstract = "Using neural networks to classify research data efficiently.",
                TechnicalStack = "Python, TensorFlow",
                ResearchAreaId = 1,
                StudentId = StudentId,
                Status = ProjectStatus.Pending
            });

            _db.SaveChanges();
        }

        // ── GetBlindProposals Tests ────────────────────────────────────────────

        [Fact]
        public async Task GetBlindProposals_ReturnsProposals_WhenNoExpertiseSet()
        {
            // Arrange: supervisor has no expertise selected — should see all proposals
            // Act
            var result = await _service.GetBlindProposalsForSupervisorAsync(SupervisorId);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetBlindProposals_FiltersBy_SupervisorExpertise()
        {
            // Arrange: supervisor specialises in area 99 (no proposals in that area)
            _db.SupervisorExpertises.Add(new SupervisorExpertise
            {
                SupervisorId = SupervisorId,
                ResearchAreaId = 99
            });
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetBlindProposalsForSupervisorAsync(SupervisorId);

            // Assert: no proposals match area 99
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetBlindProposals_DoesNotReturn_AlreadyMatchedProposals()
        {
            // Arrange: supervisor already expressed interest
            await _service.ExpressInterestAsync(SupervisorId, 1);

            // Act
            var result = await _service.GetBlindProposalsForSupervisorAsync(SupervisorId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetBlindProposals_DoesNotInclude_StudentIdentity()
        {
            // Act
            var result = await _service.GetBlindProposalsForSupervisorAsync(SupervisorId);

            // Assert: Student navigation property should NOT be eagerly loaded in blind proposals
            result.Should().HaveCount(1);
            // The BlindMatchService explicitly does NOT include Student in this query
            result[0].Student.Should().BeNull();
        }

        // ── ExpressInterest Tests ──────────────────────────────────────────────

        [Fact]
        public async Task ExpressInterest_CreatesMatch_AndSetsUnderReview()
        {
            // Act
            var result = await _service.ExpressInterestAsync(SupervisorId, 1);

            // Assert
            result.Should().BeTrue();

            var match = await _db.Matches.FirstOrDefaultAsync(m => m.ProjectProposalId == 1);
            match.Should().NotBeNull();
            match!.Status.Should().Be(MatchStatus.Interested);
            match.IdentityRevealed.Should().BeFalse();

            var project = await _db.ProjectProposals.FindAsync(1);
            project!.Status.Should().Be(ProjectStatus.UnderReview);
        }

        [Fact]
        public async Task ExpressInterest_ReturnsFalse_WhenAlreadyInterested()
        {
            // Arrange
            await _service.ExpressInterestAsync(SupervisorId, 1);

            // Act: try again
            var result = await _service.ExpressInterestAsync(SupervisorId, 1);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExpressInterest_ReturnsFalse_ForWithdrawnProject()
        {
            // Arrange: withdraw project
            await _service.WithdrawProposalAsync(StudentId, 1);

            // Act
            var result = await _service.ExpressInterestAsync(SupervisorId, 1);

            // Assert
            result.Should().BeFalse();
        }

        // ── ConfirmMatch / Identity Reveal Tests ──────────────────────────────

        [Fact]
        public async Task ConfirmMatch_RevealsIdentities_AndSetsMatched()
        {
            // Arrange
            await _service.ExpressInterestAsync(SupervisorId, 1);
            var match = await _db.Matches.FirstAsync(m => m.ProjectProposalId == 1);

            // Act
            var result = await _service.ConfirmMatchAsync(SupervisorId, match.Id);

            // Assert
            result.Should().BeTrue();

            var updatedMatch = await _db.Matches.FindAsync(match.Id);
            updatedMatch!.Status.Should().Be(MatchStatus.Confirmed);
            updatedMatch.IdentityRevealed.Should().BeTrue();          // ← KEY: reveal triggered
            updatedMatch.ConfirmedAt.Should().NotBeNull();

            var project = await _db.ProjectProposals.FindAsync(1);
            project!.Status.Should().Be(ProjectStatus.Matched);
        }

        [Fact]
        public async Task ConfirmMatch_ReturnsFalse_WhenAlreadyConfirmed()
        {
            // Arrange
            await _service.ExpressInterestAsync(SupervisorId, 1);
            var match = await _db.Matches.FirstAsync(m => m.ProjectProposalId == 1);
            await _service.ConfirmMatchAsync(SupervisorId, match.Id);

            // Act: confirm again
            var result = await _service.ConfirmMatchAsync(SupervisorId, match.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ConfirmMatch_ReturnsFalse_WhenSupervisorMismatch()
        {
            // Arrange: Supervisor2 tries to confirm Supervisor1's match
            await _service.ExpressInterestAsync(SupervisorId, 1);
            var match = await _db.Matches.FirstAsync(m => m.ProjectProposalId == 1);

            // Act
            var result = await _service.ConfirmMatchAsync(Supervisor2Id, match.Id);

            // Assert
            result.Should().BeFalse();
        }

        // ── Withdraw Tests ────────────────────────────────────────────────────

        [Fact]
        public async Task WithdrawProposal_SetsWithdrawn_WhenPending()
        {
            // Act
            var result = await _service.WithdrawProposalAsync(StudentId, 1);

            // Assert
            result.Should().BeTrue();
            var p = await _db.ProjectProposals.FindAsync(1);
            p!.Status.Should().Be(ProjectStatus.Withdrawn);
        }

        [Fact]
        public async Task WithdrawProposal_ReturnsFalse_WhenAlreadyMatched()
        {
            // Arrange: force project to matched
            var p = await _db.ProjectProposals.FindAsync(1);
            p!.Status = ProjectStatus.Matched;
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.WithdrawProposalAsync(StudentId, 1);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task WithdrawProposal_ReturnsFalse_WhenWrongStudent()
        {
            // Act
            var result = await _service.WithdrawProposalAsync("wrong-student-id", 1);

            // Assert
            result.Should().BeFalse();
        }

        // ── Reassign Tests ────────────────────────────────────────────────────

        [Fact]
        public async Task ReassignProject_UpdatesSupervisor_AndResetsReveal()
        {
            // Arrange
            await _service.ExpressInterestAsync(SupervisorId, 1);
            var match = await _db.Matches.FirstAsync(m => m.ProjectProposalId == 1);
            await _service.ConfirmMatchAsync(SupervisorId, match.Id);

            // Act: Module leader reassigns
            var result = await _service.ReassignProjectAsync(match.Id, Supervisor2Id);

            // Assert
            result.Should().BeTrue();
            var updated = await _db.Matches.FindAsync(match.Id);
            updated!.SupervisorId.Should().Be(Supervisor2Id);
            updated.IdentityRevealed.Should().BeFalse();    // reset after reassignment
            updated.Status.Should().Be(MatchStatus.Interested);
        }

        public void Dispose() => _db.Dispose();
    }
}
