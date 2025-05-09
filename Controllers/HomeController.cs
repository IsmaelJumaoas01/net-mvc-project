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

                // Basic Stats
                var totalUsers = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM USERS");
                var todaysReservations = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM facility_reservations WHERE DATE(ReservationDate) = CURDATE()");
                var pendingServices = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM service_requests WHERE Status = 'Pending'");

                // Financial Stats
                var totalBilled = connection.ExecuteScalar<decimal>(
                    "SELECT COALESCE(SUM(Amount), 0) FROM bills WHERE Status != 'Cancelled'");
                var totalCollected = connection.ExecuteScalar<decimal>(
                    "SELECT COALESCE(SUM(Amount), 0) FROM payments");
                var pendingPayments = connection.ExecuteScalar<decimal>(
                    "SELECT COALESCE(SUM(Amount), 0) FROM bills WHERE Status = 'Pending'");

                // Service Request Stats
                var totalServiceRequests = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM service_requests");
                var completedServices = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM service_requests WHERE Status = 'Completed'");
                var inProgressServices = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM service_requests WHERE Status = 'In Progress'");

                // Community Engagement Stats
                var totalAnnouncements = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM announcements");
                var totalForumPosts = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM forum_posts");
                var totalPolls = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM polls");
                var totalPollResponses = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM poll_responses");

                // Recent Activity
                var recentPayments = connection.Query<dynamic>(
                    @"SELECT p.PaymentID, p.Amount, p.PaymentDate, p.PaymentMethod, u.Username 
                    FROM payments p 
                    JOIN users u ON p.UserID = u.UserID 
                    ORDER BY p.PaymentDate DESC LIMIT 5").ToList();

                var recentServiceRequests = connection.Query<dynamic>(
                    @"SELECT sr.RequestID, sr.Description as Title, sr.Status, sr.RequestDate as CreatedAt, u.Username 
                    FROM service_requests sr 
                    JOIN users u ON sr.UserID = u.UserID 
                    ORDER BY sr.RequestDate DESC LIMIT 5").ToList();

                // Set ViewBag values
                ViewBag.TotalUsers = totalUsers;
                ViewBag.TodaysReservations = todaysReservations;
                ViewBag.PendingServices = pendingServices;
                ViewBag.TotalBilled = totalBilled;
                ViewBag.TotalCollected = totalCollected;
                ViewBag.PendingPayments = pendingPayments;
                ViewBag.TotalServiceRequests = totalServiceRequests;
                ViewBag.CompletedServices = completedServices;
                ViewBag.InProgressServices = inProgressServices;
                ViewBag.TotalAnnouncements = totalAnnouncements;
                ViewBag.TotalForumPosts = totalForumPosts;
                ViewBag.TotalPolls = totalPolls;
                ViewBag.TotalPollResponses = totalPollResponses;
                ViewBag.RecentPayments = recentPayments;
                ViewBag.RecentServiceRequests = recentServiceRequests;
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