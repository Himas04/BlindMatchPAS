using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlindMatchPAS.Tests.Integration
{
    /// <summary>
    /// Integration tests verifying database interactions, relational integrity,
    /// and end-to-end workflow of the blind match system.
    /// </summary>
    public class DatabaseIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly BlindMatchService _service;

        public DatabaseIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
            _service = new BlindMatchService(_db);
        }

        private async Task<(ApplicationUser student, ApplicationUser supervisor, ProjectProposal proposal)> SetupFullScenarioAsync()
        {
            var student = new ApplicationUser
            {
                Id = "s1", FullName = "Test Student", Email = "student@test.com",
                UserName = "student@test.com", Role = "Student"
            };
            var supervisor = new ApplicationUser
            {
                Id = "sv1", FullName = "Test Supervisor", Email = "supervisor@test.com",
                UserName = "supervisor@test.com", Role = "Supervisor"
            };

            _db.Users.AddRange(student, supervisor);
            _db.ResearchAreas.Add(new ResearchArea { Id = 10, Name = "Cybersecurity", IsActive = true });

            var proposal = new ProjectProposal
            {
                Id = 10,
                Title = "Zero Trust Architecture",
                Abstract = "Implementing zero trust security model for enterprise networks.",
                TechnicalStack = "ASP.NET Core, Azure AD",
                ResearchAreaId = 10,
                StudentId = "s1",
                Status = ProjectStatus.Pending
            };
            _db.ProjectProposals.Add(proposal);
            await _db.SaveChangesAsync();

            return (student, supervisor, proposal);
        }

        // ── Full Workflow Integration Test ────────────────────────────────────

        [Fact]
        public async Task FullWorkflow_PendingToMatchedWithReveal_Succeeds()
        {
            // Arrange
            var (student, supervisor, proposal) = await SetupFullScenarioAsync();

            // Step 1: Supervisor expresses interest (blind)
            var interested = await _service.ExpressInterestAsync(supervisor.Id, proposal.Id);
            interested.Should().BeTrue();

            // Step 2: Verify proposal is under review
            var underReview = await _db.ProjectProposals.FindAsync(proposal.Id);
            underReview!.Status.Should().Be(ProjectStatus.UnderReview);

            // Step 3: Verify match created WITHOUT identity reveal
            var match = await _db.Matches.FirstAsync(m => m.ProjectProposalId == proposal.Id);
            match.IdentityRevealed.Should().BeFalse();
            match.Status.Should().Be(MatchStatus.Interested);

            // Step 4: Supervisor confirms match — triggers IDENTITY REVEAL
            var confirmed = await _service.ConfirmMatchAsync(supervisor.Id, match.Id);
            confirmed.Should().BeTrue();

            // Step 5: Verify identity revealed
            var finalMatch = await _db.Matches
                .Include(m => m.ProjectProposal).ThenInclude(p => p!.Student)
                .Include(m => m.Supervisor)
                .FirstAsync(m => m.Id == match.Id);

            finalMatch.IdentityRevealed.Should().BeTrue();
            finalMatch.Status.Should().Be(MatchStatus.Confirmed);
            finalMatch.ProjectProposal!.Status.Should().Be(ProjectStatus.Matched);

            // Step 6: Verify student and supervisor details accessible post-reveal
            finalMatch.ProjectProposal.Student!.FullName.Should().Be("Test Student");
            finalMatch.Supervisor!.FullName.Should().Be("Test Supervisor");
        }

        // ── ResearchArea Filtering Integration Test ───────────────────────────

        [Fact]
        public async Task SupervisorWithExpertise_OnlySeesMatchingProposals()
        {
            // Arrange
            var (_, supervisor, _) = await SetupFullScenarioAsync();

            // Add a second proposal in a different area
            _db.ResearchAreas.Add(new ResearchArea { Id = 20, Name = "Web Dev", IsActive = true });
            _db.ProjectProposals.Add(new ProjectProposal
            {
                Id = 20,
                Title = "E-Commerce Platform",
                Abstract = "Building a scalable e-commerce platform with microservices.",
                TechnicalStack = "React, Node.js",
                ResearchAreaId = 20,
                StudentId = "s1",
                Status = ProjectStatus.Pending
            });

            // Set supervisor expertise to Cybersecurity (area 10) only
            _db.SupervisorExpertises.Add(new SupervisorExpertise
            {
                SupervisorId = supervisor.Id,
                ResearchAreaId = 10
            });
            await _db.SaveChangesAsync();

            // Act
            var visible = await _service.GetBlindProposalsForSupervisorAsync(supervisor.Id);

            // Assert: only the Cybersecurity proposal is visible
            visible.Should().HaveCount(1);
            visible[0].ResearchAreaId.Should().Be(10);
        }

        // ── Data Persistence Test ─────────────────────────────────────────────

        [Fact]
        public async Task MatchTimestamps_AreRecorded_Correctly()
        {
            // Arrange
            var (_, supervisor, proposal) = await SetupFullScenarioAsync();
            var before = DateTime.UtcNow;

            // Act
            await _service.ExpressInterestAsync(supervisor.Id, proposal.Id);
            var match = await _db.Matches.FirstAsync(m => m.ProjectProposalId == proposal.Id);
            await _service.ConfirmMatchAsync(supervisor.Id, match.Id);

            // Assert
            var final = await _db.Matches.FindAsync(match.Id);
            final!.CreatedAt.Should().BeOnOrAfter(before);
            final.ConfirmedAt.Should().NotBeNull();
            final.ConfirmedAt!.Value.Should().BeOnOrAfter(final.CreatedAt);
        }

        // ── Cascade / Relational Integrity Test ──────────────────────────────

        [Fact]
        public async Task GetStudentProposals_IncludesMatchAndSupervisorInfo_AfterReveal()
        {
            // Arrange
            var (student, supervisor, proposal) = await SetupFullScenarioAsync();
            await _service.ExpressInterestAsync(supervisor.Id, proposal.Id);
            var match = await _db.Matches.FirstAsync(m => m.ProjectProposalId == proposal.Id);
            await _service.ConfirmMatchAsync(supervisor.Id, match.Id);

            // Act: get from student's perspective
            var proposals = await _service.GetStudentProposalsAsync(student.Id);

            // Assert: student can see supervisor details via revealed match
            proposals.Should().HaveCount(1);
            proposals[0].Match.Should().NotBeNull();
            proposals[0].Match!.IdentityRevealed.Should().BeTrue();
            proposals[0].Match.Supervisor!.Email.Should().Be("supervisor@test.com");
        }

        // ── Multiple Supervisors Test ─────────────────────────────────────────

        [Fact]
        public async Task OnlyOneMatch_CanBeConfirmed_PerProject()
        {
            // Arrange
            var (_, supervisor, proposal) = await SetupFullScenarioAsync();
            var supervisor2 = new ApplicationUser
            {
                Id = "sv2", FullName = "Second Supervisor", Email = "sv2@test.com",
                UserName = "sv2@test.com", Role = "Supervisor"
            };
            _db.Users.Add(supervisor2);
            await _db.SaveChangesAsync();

            // Supervisor 1 expresses interest and confirms
            await _service.ExpressInterestAsync(supervisor.Id, proposal.Id);
            var match1 = await _db.Matches.FirstAsync(m => m.SupervisorId == supervisor.Id);
            await _service.ConfirmMatchAsync(supervisor.Id, match1.Id);

            // Supervisor 2 tries to express interest on already-matched project
            var result = await _service.ExpressInterestAsync(supervisor2.Id, proposal.Id);

            // Assert
            result.Should().BeFalse();
            var matchCount = await _db.Matches.CountAsync(m => m.ProjectProposalId == proposal.Id);
            matchCount.Should().Be(1);
        }

        public void Dispose() => _db.Dispose();
    }
}
