using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Controllers
{
    [Authorize(Roles = "Supervisor")]
    public class SupervisorController : Controller
    {
        private readonly IBlindMatchService _matchService;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupervisorController(IBlindMatchService matchService,
                                    ApplicationDbContext db,
                                    UserManager<ApplicationUser> userManager)
        {
            _matchService = matchService;
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User)!;

            var selectedAreaIds = await _db.SupervisorExpertises
                .Where(se => se.SupervisorId == userId)
                .Select(se => se.ResearchAreaId)
                .ToListAsync();

            var blindProposals = await _matchService.GetBlindProposalsForSupervisorAsync(userId);
            var myMatches = await _matchService.GetSupervisorMatchesAsync(userId);

            var vm = new SupervisorDashboardViewModel
            {
                AllAreas = await _db.ResearchAreas.Where(r => r.IsActive).ToListAsync(),
                SelectedAreaIds = selectedAreaIds,
                AvailableProposals = blindProposals.Select(p => new BlindProposalViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Abstract = p.Abstract,
                    TechnicalStack = p.TechnicalStack,
                    ResearchArea = p.ResearchArea?.Name ?? "",
                    Status = p.Status,
                    AlreadyExpressedInterest = false
                }).ToList(),
                MyMatches = myMatches.Select(m => new MatchViewModel
                {
                    MatchId = m.Id,
                    ProjectId = m.ProjectProposalId,
                    ProjectTitle = m.ProjectProposal?.Title ?? "",
                    ResearchArea = m.ProjectProposal?.ResearchArea?.Name ?? "",
                    Status = m.Status,
                    IdentityRevealed = m.IdentityRevealed,
                    StudentName = m.IdentityRevealed ? m.ProjectProposal?.Student?.FullName : null,
                    StudentEmail = m.IdentityRevealed ? m.ProjectProposal?.Student?.Email : null
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> SaveExpertise(List<int> areaIds)
        {
            var userId = _userManager.GetUserId(User)!;

            var existing = _db.SupervisorExpertises.Where(se => se.SupervisorId == userId);
            _db.SupervisorExpertises.RemoveRange(existing);

            if (areaIds != null)
            {
                foreach (var areaId in areaIds)
                {
                    _db.SupervisorExpertises.Add(new SupervisorExpertise
                    {
                        SupervisorId = userId,
                        ResearchAreaId = areaId
                    });
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Expertise preferences saved!";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> ExpressInterest(int projectId)
        {
            var userId = _userManager.GetUserId(User)!;
            var success = await _matchService.ExpressInterestAsync(userId, projectId);

            TempData[success ? "Success" : "Error"] = success
                ? "Interest expressed! Project is now under review."
                : "Could not express interest — project may already be matched.";

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmMatch(int matchId)
        {
            var userId = _userManager.GetUserId(User)!;
            var success = await _matchService.ConfirmMatchAsync(userId, matchId);

            TempData[success ? "Success" : "Error"] = success
                ? "Match confirmed! Identity revealed — check your matches."
                : "Could not confirm match.";

            return RedirectToAction("Dashboard");
        }
    }
}
