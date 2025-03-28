using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace homeowner.Controllers
{
    [Route("announcements")]
    public class AnnouncementController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";
        private readonly ILogger<AnnouncementController> _logger;

        public AnnouncementController(ILogger<AnnouncementController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
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

            // Get the role from session
            var role = HttpContext.Session.GetString("Role");
            ViewBag.Role = role;

            if (role == "Homeowner")
            {
                // Return the homeowner view (which is the partial view with full page markup)
                return View("~/Views/Homeowner/_HomeownerAnnouncements.cshtml", announcements);
            }
            else
            {
                // Return the admin/staff view as before
                return View("~/Views/Admin_Staff/Index.cshtml", announcements);
            }
        }


        [HttpPost]
        [Route("create")]
        public IActionResult Create(AnnouncementModel announcement)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            announcement.UserID = int.Parse(HttpContext.Session.GetString("UserID"));

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO announcements (Title, Content, UserID) VALUES (@Title, @Content, @UserID)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Title", announcement.Title);
                cmd.Parameters.AddWithValue("@Content", announcement.Content);
                cmd.Parameters.AddWithValue("@UserID", announcement.UserID);

                cmd.ExecuteNonQuery();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [Route("edit")]
        public IActionResult Edit(AnnouncementModel announcement)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE announcements SET Title = @Title, Content = @Content WHERE AnnouncementID = @AnnouncementID";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Title", announcement.Title);
                cmd.Parameters.AddWithValue("@Content", announcement.Content);
                cmd.Parameters.AddWithValue("@AnnouncementID", announcement.AnnouncementID);

                int rowsAffected = cmd.ExecuteNonQuery();
                _logger.LogInformation($"Edit: Rows affected: {rowsAffected}");
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [Route("delete")]
        public IActionResult Delete(int AnnouncementID)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            _logger.LogInformation($"Deleting announcement with ID: {AnnouncementID}");

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM announcements WHERE AnnouncementID = @AnnouncementID";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@AnnouncementID", AnnouncementID);

                int rowsAffected = cmd.ExecuteNonQuery();
                _logger.LogInformation($"Delete: Rows affected: {rowsAffected}");
            }

            return Json(new { success = true });
        }
    }
}