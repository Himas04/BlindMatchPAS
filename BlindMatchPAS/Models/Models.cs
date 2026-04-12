using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Models
{
    // ─── Identity User ───────────────────────────────────────────────────────────
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty; // Student | Supervisor | ModuleLeader | Admin
    }

    // ─── Research Area ────────────────────────────────────────────────────────────
    public class ResearchArea
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<ProjectProposal> Projects { get; set; } = new List<ProjectProposal>();
        public ICollection<SupervisorExpertise> SupervisorExpertises { get; set; } = new List<SupervisorExpertise>();
    }

    // ─── Project Proposal ─────────────────────────────────────────────────────────
    public class ProjectProposal
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Abstract { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string TechnicalStack { get; set; } = string.Empty;

        public int ResearchAreaId { get; set; }
        public ResearchArea? ResearchArea { get; set; }

        // Owner (student)
        public string StudentId { get; set; } = string.Empty;
        public ApplicationUser? Student { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.Pending;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Match
        public Match? Match { get; set; }
    }

    public enum ProjectStatus
    {
        Pending,
        UnderReview,
        Matched,
        Withdrawn
    }

    // ─── Supervisor Expertise ─────────────────────────────────────────────────────
    public class SupervisorExpertise
    {
        public int Id { get; set; }
        public string SupervisorId { get; set; } = string.Empty;
        public ApplicationUser? Supervisor { get; set; }
        public int ResearchAreaId { get; set; }
        public ResearchArea? ResearchArea { get; set; }
    }

    // ─── Match ────────────────────────────────────────────────────────────────────
    public class Match
    {
        public int Id { get; set; }

        public int ProjectProposalId { get; set; }
        public ProjectProposal? ProjectProposal { get; set; }

        public string SupervisorId { get; set; } = string.Empty;
        public ApplicationUser? Supervisor { get; set; }

        public MatchStatus Status { get; set; } = MatchStatus.Interested;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ConfirmedAt { get; set; }

        // Reveal flag — once confirmed, identities are visible
        public bool IdentityRevealed { get; set; } = false;
    }

    public enum MatchStatus
    {
        Interested,
        Confirmed,
        Rejected
    }
}
