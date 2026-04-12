using Microsoft.AspNetCore.Mvc;

namespace BlindMatchPAS.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Student"))      return RedirectToAction("Dashboard", "Student");
                if (User.IsInRole("Supervisor"))   return RedirectToAction("Dashboard", "Supervisor");
                if (User.IsInRole("ModuleLeader")) return RedirectToAction("Dashboard", "ModuleLeader");
                if (User.IsInRole("Admin"))        return RedirectToAction("Dashboard", "Admin");
            }
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
