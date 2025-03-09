using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System.Collections.Generic;
using Dapper;

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

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM announcements ORDER BY CreatedAt DESC";
                announcements = conn.Query<AnnouncementModel>(query).ToList();
            }

            ViewBag.Announcements = announcements;

            return View();
        }

        public IActionResult StaffDashboard()
        {
            if (!IsStaff())
            {
                return RedirectToAction("Index");
            }

            using (var connection = new MySqlConnection(connectionString))
            {
                // Get today's reservations count
                var todaysReservations = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM facility_reservations WHERE DATE(ReservationDate) = CURDATE()");

                // Get pending service requests count
                var pendingServices = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM service_requests WHERE Status = 'Pending'");

                // Get new feedback count (feedback from last 24 hours)
                var newFeedback = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM feedback WHERE CreatedAt >= DATE_SUB(NOW(), INTERVAL 24 HOUR)");

                ViewBag.TodaysReservations = todaysReservations;
                ViewBag.PendingServices = pendingServices;
                ViewBag.NewFeedback = newFeedback;
            }

            return View();
        }

        public IActionResult AdminDashboard()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Get total users count
                var totalUsers = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM USERS");

                // Get today's reservations count
                var todaysReservations = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM facility_reservations WHERE DATE(ReservationDate) = CURDATE()");

                // Get pending service requests count
                var pendingServices = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM service_requests WHERE Status = 'Pending'");

                ViewBag.TotalUsers = totalUsers;
                ViewBag.TodaysReservations = todaysReservations;
                ViewBag.PendingServices = pendingServices;
            }

            return View();
        }

        public IActionResult Profile()
        {
            string userId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "You must be logged in to view your profile.";
                return RedirectToAction("Login", "Account");
            }

            UserModel user = null;
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM USERS WHERE UserID = @UserID";
                user = conn.QueryFirstOrDefault<UserModel>(query, new { UserID = userId });
            }

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            return View(user);
        }

        private bool IsAdmin()
        {
            string role = HttpContext.Session.GetString("Role");
            return role == "Administrator";
        }

        private bool IsStaff()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Staff";
        }
    }
}