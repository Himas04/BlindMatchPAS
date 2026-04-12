using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Models
{
    // ─── Auth View Models ─────────────────────────────────────────────────────────
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Student";
    }

    // ─── Student View Models ──────────────────────────────────────────────────────
    public class SubmitProposalViewModel
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Abstract { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string TechnicalStack { get; set; } = string.Empty;

        [Required]
        public int ResearchAreaId { get; set; }

        public IEnumerable<ResearchArea> ResearchAreas { get; set; } = new List<ResearchArea>();
    }

    public class StudentDashboardViewModel
    {
        public List<ProjectProposalViewModel> Proposals { get; set; } = new();
    }

    public class ProjectProposalViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string TechnicalStack { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;
        public ProjectStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
        // Reveal info (only populated after match confirmed)
        public string? SupervisorName { get; set; }
        public string? SupervisorEmail { get; set; }
        public bool IdentityRevealed { get; set; }
    }

    // ─── Supervisor View Models ───────────────────────────────────────────────────
    public class BlindProposalViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string TechnicalStack { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;
        public ProjectStatus Status { get; set; }
        // No student info — blind review
        public bool AlreadyExpressedInterest { get; set; }
    }

    public class SupervisorDashboardViewModel
    {
        public List<BlindProposalViewModel> AvailableProposals { get; set; } = new();
        public List<MatchViewModel> MyMatches { get; set; } = new();
        public List<ResearchArea> AllAreas { get; set; } = new();
        public List<int> SelectedAreaIds { get; set; } = new();
    }

    public class MatchViewModel
    {
        public int MatchId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public string ResearchArea { get; set; } = string.Empty;
        public MatchStatus Status { get; set; }
        public bool IdentityRevealed { get; set; }
        // Revealed after confirmation
        public string? StudentName { get; set; }
        public string? StudentEmail { get; set; }
    }

    // ─── Module Leader View Models ────────────────────────────────────────────────
    public class AdminDashboardViewModel
    {
        public int TotalProposals { get; set; }
        public int TotalMatches { get; set; }
        public int PendingProposals { get; set; }
        public int ConfirmedMatches { get; set; }
        public List<AllMatchViewModel> AllMatches { get; set; } = new();
        public List<UserViewModel> AllUsers { get; set; } = new();
        public List<ResearchArea> ResearchAreas { get; set; } = new();
    }

    public class AllMatchViewModel
    {
        public int MatchId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public string SupervisorEmail { get; set; } = string.Empty;
        public MatchStatus MatchStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class CreateResearchAreaViewModel
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }
    }
}
