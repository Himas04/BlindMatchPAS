using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly IBlindMatchService _matchService;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(IBlindMatchService matchService,
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
            var proposals = await _matchService.GetStudentProposalsAsync(userId);

            var vm = new StudentDashboardViewModel
            {
                Proposals = proposals.Select(p => new ProjectProposalViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Abstract = p.Abstract,
                    TechnicalStack = p.TechnicalStack,
                    ResearchArea = p.ResearchArea?.Name ?? "",
                    Status = p.Status,
                    SubmittedAt = p.SubmittedAt,
                    IdentityRevealed = p.Match?.IdentityRevealed ?? false,
                    SupervisorName = (p.Match?.IdentityRevealed == true) ? p.Match.Supervisor?.FullName : null,
                    SupervisorEmail = (p.Match?.IdentityRevealed == true) ? p.Match.Supervisor?.Email : null,
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Submit()
        {
            var vm = new SubmitProposalViewModel
            {
                ResearchAreas = await _db.ResearchAreas.Where(r => r.IsActive).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(SubmitProposalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ResearchAreas = await _db.ResearchAreas.Where(r => r.IsActive).ToListAsync();
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;
            var proposal = new ProjectProposal
            {
                Title = model.Title,
                Abstract = model.Abstract,
                TechnicalStack = model.TechnicalStack,
                ResearchAreaId = model.ResearchAreaId,
                StudentId = userId,
                Status = ProjectStatus.Pending
            };

            _db.ProjectProposals.Add(proposal);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Proposal submitted successfully!";
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var proposal = await _matchService.GetProposalByIdAsync(id);

            if (proposal == null || proposal.StudentId != userId || proposal.Status == ProjectStatus.Matched)
                return NotFound();

            var vm = new SubmitProposalViewModel
            {
                Title = proposal.Title,
                Abstract = proposal.Abstract,
                TechnicalStack = proposal.TechnicalStack,
                ResearchAreaId = proposal.ResearchAreaId,
                ResearchAreas = await _db.ResearchAreas.Where(r => r.IsActive).ToListAsync()
            };

            ViewBag.ProposalId = id;
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, SubmitProposalViewModel model)
        {
            var userId = _userManager.GetUserId(User)!;
            var proposal = await _db.ProjectProposals
                .FirstOrDefaultAsync(p => p.Id == id && p.StudentId == userId);

            if (proposal == null || proposal.Status == ProjectStatus.Matched)
                return NotFound();

            if (!ModelState.IsValid)
            {
                model.ResearchAreas = await _db.ResearchAreas.Where(r => r.IsActive).ToListAsync();
                ViewBag.ProposalId = id;
                return View(model);
            }

            proposal.Title = model.Title;
            proposal.Abstract = model.Abstract;
            proposal.TechnicalStack = model.TechnicalStack;
            proposal.ResearchAreaId = model.ResearchAreaId;
            proposal.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Proposal updated successfully!";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> Withdraw(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var success = await _matchService.WithdrawProposalAsync(userId, id);

            TempData[success ? "Success" : "Error"] = success
                ? "Proposal withdrawn."
                : "Cannot withdraw a matched proposal.";

            return RedirectToAction("Dashboard");
        }
    }
}
