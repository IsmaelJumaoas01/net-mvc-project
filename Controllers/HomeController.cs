using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace homeowner.Controllers
{
    public class HomeController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";

        public IActionResult Index()
        {
            ViewBag.IsLoggedIn = HttpContext.Session.GetString("UserID") != null;
            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Guest";
            ViewBag.Role = HttpContext.Session.GetString("Role") ?? "Homeowner";

            if (ViewBag.Role == "Staff")
            {
                return RedirectToAction("StaffDashboard");
            }
            else if (ViewBag.Role == "Administrator")
            {
                return RedirectToAction("AdminDashboard");
            }

            return View();
        }

        public IActionResult StaffDashboard()
        {
            return View();
        }

        public IActionResult AdminDashboard()
        {
            return View();
        }
    }
}