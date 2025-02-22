using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System.Collections.Generic;

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

            List<AnnouncementModel> announcements = new List<AnnouncementModel>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM announcements ORDER BY CreatedAt DESC";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    announcements.Add(new AnnouncementModel
                    {
                        AnnouncementID = reader.GetInt32("AnnouncementID"),
                        Title = reader.GetString("Title"),
                        Content = reader.GetString("Content"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UserID = reader.GetInt32("UserID")
                    });
                }
            }

            ViewBag.Announcements = announcements;

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