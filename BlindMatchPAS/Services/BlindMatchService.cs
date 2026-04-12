using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Services
{
    public interface IBlindMatchService
    {
        Task<List<ProjectProposal>> GetBlindProposalsForSupervisorAsync(string supervisorId);
        Task<bool> ExpressInterestAsync(string supervisorId, int projectId);
        Task<bool> ConfirmMatchAsync(string supervisorId, int matchId);
        Task<Match?> GetMatchByIdAsync(int matchId);
        Task<List<Match>> GetSupervisorMatchesAsync(string supervisorId);
        Task<List<ProjectProposal>> GetStudentProposalsAsync(string studentId);
        Task<ProjectProposal?> GetProposalByIdAsync(int id);
        Task<bool> WithdrawProposalAsync(string studentId, int proposalId);
        Task<bool> ReassignProjectAsync(int matchId, string newSupervisorId);
    }

    public class BlindMatchService : IBlindMatchService
    {
        private readonly ApplicationDbContext _db;

        public BlindMatchService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Returns proposals filtered by supervisor's preferred research areas,
        /// excluding already matched or withdrawn projects.
        /// Student identity is NOT included — blind review.
        /// </summary>
        public async Task<List<ProjectProposal>> GetBlindProposalsForSupervisorAsync(string supervisorId)
        {
            var expertiseAreaIds = await _db.SupervisorExpertises
                .Where(se => se.SupervisorId == supervisorId)
                .Select(se => se.ResearchAreaId)
                .ToListAsync();

            // Only show Pending proposals — UnderReview means another supervisor
            // has already expressed interest, so it should not appear to others.
            var query = _db.ProjectProposals
                .Include(p => p.ResearchArea)
                .Where(p => p.Status == ProjectStatus.Pending);

            // Filter by expertise if supervisor has set preferences
            if (expertiseAreaIds.Any())
                query = query.Where(p => expertiseAreaIds.Contains(p.ResearchAreaId));

            // Exclude proposals where this supervisor already expressed interest
            var alreadyInterestedIds = await _db.Matches
                .Where(m => m.SupervisorId == supervisorId)
                .Select(m => m.ProjectProposalId)
                .ToListAsync();

            return await query
                .Where(p => !alreadyInterestedIds.Contains(p.Id))
                .OrderByDescending(p => p.SubmittedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Supervisor expresses interest — creates a Match with status Interested
        /// and sets project status to UnderReview.
        /// </summary>
        public async Task<bool> ExpressInterestAsync(string supervisorId, int projectId)
        {
            var project = await _db.ProjectProposals.FindAsync(projectId);
            if (project == null || project.Status == ProjectStatus.Matched || project.Status == ProjectStatus.Withdrawn)
                return false;

            var alreadyExists = await _db.Matches
                .AnyAsync(m => m.SupervisorId == supervisorId && m.ProjectProposalId == projectId);
            if (alreadyExists) return false;

            var match = new Match
            {
                SupervisorId = supervisorId,
                ProjectProposalId = projectId,
                Status = MatchStatus.Interested,
                IdentityRevealed = false
            };

            project.Status = ProjectStatus.UnderReview;
            project.UpdatedAt = DateTime.UtcNow;

            _db.Matches.Add(match);
            await _db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Supervisor confirms a match — triggers Identity Reveal.
        /// Sets project to Matched and reveals both parties' identities.
        /// </summary>
        public async Task<bool> ConfirmMatchAsync(string supervisorId, int matchId)
        {
            var match = await _db.Matches
                .Include(m => m.ProjectProposal)
                .FirstOrDefaultAsync(m => m.Id == matchId && m.SupervisorId == supervisorId);

            if (match == null || match.Status == MatchStatus.Confirmed)
                return false;

            // ── IDENTITY REVEAL ──────────────────────────────────────────────────
            match.Status = MatchStatus.Confirmed;
            match.ConfirmedAt = DateTime.UtcNow;
            match.IdentityRevealed = true;

            if (match.ProjectProposal != null)
            {
                match.ProjectProposal.Status = ProjectStatus.Matched;
                match.ProjectProposal.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<Match?> GetMatchByIdAsync(int matchId)
        {
            return await _db.Matches
                .Include(m => m.ProjectProposal).ThenInclude(p => p!.ResearchArea)
                .Include(m => m.ProjectProposal).ThenInclude(p => p!.Student)
                .Include(m => m.Supervisor)
                .FirstOrDefaultAsync(m => m.Id == matchId);
        }

        public async Task<List<Match>> GetSupervisorMatchesAsync(string supervisorId)
        {
            return await _db.Matches
                .Include(m => m.ProjectProposal).ThenInclude(p => p!.ResearchArea)
                .Include(m => m.ProjectProposal).ThenInclude(p => p!.Student)
                .Where(m => m.SupervisorId == supervisorId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ProjectProposal>> GetStudentProposalsAsync(string studentId)
        {
            return await _db.ProjectProposals
                .Include(p => p.ResearchArea)
                .Include(p => p.Match).ThenInclude(m => m!.Supervisor)
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.SubmittedAt)
                .ToListAsync();
        }

        public async Task<ProjectProposal?> GetProposalByIdAsync(int id)
        {
            return await _db.ProjectProposals
                .Include(p => p.ResearchArea)
                .Include(p => p.Student)
                .Include(p => p.Match)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> WithdrawProposalAsync(string studentId, int proposalId)
        {
            var proposal = await _db.ProjectProposals
                .FirstOrDefaultAsync(p => p.Id == proposalId && p.StudentId == studentId);

            if (proposal == null || proposal.Status == ProjectStatus.Matched)
                return false;

            proposal.Status = ProjectStatus.Withdrawn;
            proposal.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReassignProjectAsync(int matchId, string newSupervisorId)
        {
            var match = await _db.Matches
                .Include(m => m.ProjectProposal)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null) return false;

            match.SupervisorId = newSupervisorId;
            match.Status = MatchStatus.Interested;
            match.IdentityRevealed = false;
            match.ConfirmedAt = null;

            if (match.ProjectProposal != null)
                match.ProjectProposal.Status = ProjectStatus.UnderReview;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
