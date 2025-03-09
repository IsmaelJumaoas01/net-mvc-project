using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Dapper;
using MySql.Data.MySqlClient;
using System.Data;
using homeowner.Models;

namespace homeowner.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly string _connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";

        public IActionResult Index()
        {
            string role = HttpContext.Session.GetString("Role") ?? "";
            ViewBag.IsAdmin = role == "Administrator";
            ViewBag.IsStaff = role == "Staff";

            List<FeedbackModel> feedbacks = new List<FeedbackModel>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string sql = @"SELECT f.*, u.Username 
                              FROM feedback f
                              JOIN users u ON f.UserID = u.UserID
                              ORDER BY f.CreatedAt DESC";
                feedbacks = connection.Query<FeedbackModel>(sql).ToList();
            }

            return View(feedbacks);
        }

        // GET: Feedback/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Feedback/Create
        [HttpPost]
        public IActionResult Create([FromBody] FeedbackModel feedback)
        {
            try
            {
                if (string.IsNullOrEmpty(feedback.Title) || string.IsNullOrEmpty(feedback.Content))
                {
                    return Json(new { success = false, message = "Title and content are required." });
                }

                string userIdStr = HttpContext.Session.GetString("UserID");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var sql = @"
                        INSERT INTO feedback (UserID, Title, Content, Status, CreatedAt) 
                        VALUES (@UserID, @Title, @Content, 'Pending', NOW())";

                    connection.Execute(sql, new
                    {
                        UserID = int.Parse(userIdStr),
                        feedback.Title,
                        feedback.Content
                    });

                    return Json(new { success = true, message = "Feedback submitted successfully" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // GET: Feedback/Edit/5
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Edit(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                var feedback = connection.QueryFirstOrDefault<FeedbackModel>(@"
                    SELECT f.*, u.Username 
                    FROM feedback f 
                    JOIN USERS u ON f.UserID = u.UserID 
                    WHERE f.FeedbackID = @FeedbackID", new { FeedbackID = id });

                if (feedback == null)
                {
                    return NotFound();
                }

                return View(feedback);
            }
        }

        // POST: Feedback/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Edit(int id, FeedbackModel feedback)
        {
            if (id != feedback.FeedbackID)
            {
                return Json(new { success = false, message = "Invalid feedback ID" });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid feedback data" });
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                var sql = @"
                    UPDATE feedback 
                    SET Title = @Title, 
                        Content = @Content, 
                        Status = @Status,
                        UpdatedAt = NOW()
                    WHERE FeedbackID = @FeedbackID";

                connection.Execute(sql, new
                {
                    feedback.FeedbackID,
                    feedback.Title,
                    feedback.Content,
                    feedback.Status
                });

                return Json(new { success = true });
            }
        }

        // POST: Feedback/Delete/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Execute("DELETE FROM feedback WHERE FeedbackID = @FeedbackID", new { FeedbackID = id });
                return Json(new { success = true });
            }
        }

        // POST: Feedback/UpdateStatus/5
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult UpdateStatus(int id, string status)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Execute(@"
                    UPDATE feedback 
                    SET Status = @Status,
                        UpdatedAt = NOW()
                    WHERE FeedbackID = @FeedbackID", 
                    new { FeedbackID = id, Status = status });

                return Json(new { success = true });
            }
        }
    }
} 