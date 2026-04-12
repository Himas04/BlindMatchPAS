using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Controllers
{
    [Authorize(Roles = "ModuleLeader")]
    public class ModuleLeaderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBlindMatchService _matchService;

        public ModuleLeaderController(ApplicationDbContext db,
                                      UserManager<ApplicationUser> userManager,
                                      IBlindMatchService matchService)
        {
            _db = db;
            _userManager = userManager;
            _matchService = matchService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var allMatches = await _db.Matches
                .Include(m => m.ProjectProposal).ThenInclude(p => p!.Student)
                .Include(m => m.ProjectProposal).ThenInclude(p => p!.ResearchArea)
                .Include(m => m.Supervisor)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var allUsers = await _userManager.Users.ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                TotalProposals = await _db.ProjectProposals.CountAsync(),
                TotalMatches = allMatches.Count,
                PendingProposals = await _db.ProjectProposals.CountAsync(p => p.Status == ProjectStatus.Pending),
                ConfirmedMatches = allMatches.Count(m => m.Status == MatchStatus.Confirmed),
                ResearchAreas = await _db.ResearchAreas.ToListAsync(),
                AllMatches = allMatches.Select(m => new AllMatchViewModel
                {
                    MatchId = m.Id,
                    ProjectTitle = m.ProjectProposal?.Title ?? "",
                    StudentName = m.ProjectProposal?.Student?.FullName ?? "",
                    StudentEmail = m.ProjectProposal?.Student?.Email ?? "",
                    SupervisorName = m.Supervisor?.FullName ?? "",
                    SupervisorEmail = m.Supervisor?.Email ?? "",
                    MatchStatus = m.Status,
                    CreatedAt = m.CreatedAt
                }).ToList(),
                AllUsers = allUsers.Select(u => new UserViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Role = u.Role
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Reassign(int matchId, string newSupervisorId)
        {
            var success = await _matchService.ReassignProjectAsync(matchId, newSupervisorId);
            TempData[success ? "Success" : "Error"] = success
                ? "Project reassigned successfully."
                : "Failed to reassign project.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> AddResearchArea(CreateResearchAreaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid research area data.";
                return RedirectToAction("Dashboard");
            }

            _db.ResearchAreas.Add(new ResearchArea
            {
                Name = model.Name,
                Description = model.Description,
                IsActive = true
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Research area added!";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleResearchArea(int id)
        {
            var area = await _db.ResearchAreas.FindAsync(id);
            if (area != null)
            {
                area.IsActive = !area.IsActive;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Dashboard");
        }
    }
}
