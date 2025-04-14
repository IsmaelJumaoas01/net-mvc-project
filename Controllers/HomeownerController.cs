using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using homeowner.Models;

namespace homeowner.Controllers
{
    public class HomeownerController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";
        private readonly ILogger<HomeownerController> _logger;

        public HomeownerController(ILogger<HomeownerController> logger)
        {
            _logger = logger;
        }
        
        public IActionResult Profile()
        {
            string userId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "You must be logged in to view your profile.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.IsLoggedIn = true;

            UserModel user = null;
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM USERS WHERE UserID = @UserID";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    user = new UserModel
                    {
                        UserID = reader.GetInt32("UserID"),
                        Username = reader.GetString("Username"),
                        Email = reader.GetString("Email"),
                        FirstName = reader["FirstName"] as string,
                        MiddleName = reader["MiddleName"] as string,
                        LastName = reader["LastName"] as string,
                        PhoneNumber = reader["PhoneNumber"] as string,
                        Role = reader.GetString("Role"),
                        CreatedAt = reader.GetDateTime("CreatedAt")
                    };
                }
            }

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            return View("/Views/Homeowner/Profile.cshtml", user);
        }

        [HttpPost]
        public IActionResult EditProfile(string username, string email, string firstName, string middleName, string lastName, string phoneNumber, string password, string confirmPassword)
        {
            string userId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "You must be logged in to update your profile." });
            }

            if (!string.IsNullOrEmpty(password) && password != confirmPassword)
            {
                return Json(new { success = false, message = "Passwords do not match." });
            }

            _logger.LogInformation("User {UserId} is attempting to update their profile.", userId);

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Check for duplicate username
                    if (!string.IsNullOrEmpty(username))
                    {
                        string checkQuery = "SELECT UserID FROM USERS WHERE Username = @Username AND UserID != @UserID";
                        MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                        checkCmd.Parameters.AddWithValue("@Username", username);
                        checkCmd.Parameters.AddWithValue("@UserID", userId);
                        var result = checkCmd.ExecuteScalar();
                        if (result != null)
                        {
                            return Json(new { success = false, message = "Username already exists." });
                        }
                    }

                    // Check for duplicate email
                    if (!string.IsNullOrEmpty(email))
                    {
                        string checkQuery = "SELECT UserID FROM USERS WHERE Email = @Email AND UserID != @UserID";
                        MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                        checkCmd.Parameters.AddWithValue("@Email", email);
                        checkCmd.Parameters.AddWithValue("@UserID", userId);
                        var result = checkCmd.ExecuteScalar();
                        if (result != null)
                        {
                            return Json(new { success = false, message = "Email already exists." });
                        }
                    }

                    // Check for duplicate phone number
                    if (!string.IsNullOrEmpty(phoneNumber))
                    {
                        string checkQuery = "SELECT UserID FROM USERS WHERE PhoneNumber = @PhoneNumber AND UserID != @UserID";
                        MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                        checkCmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                        checkCmd.Parameters.AddWithValue("@UserID", userId);
                        var result = checkCmd.ExecuteScalar();
                        if (result != null)
                        {
                            return Json(new { success = false, message = "Phone number already exists." });
                        }
                    }

                    // Build update query dynamically based on provided values
                    var updateFields = new List<string>();
                    var parameters = new List<MySqlParameter>();

                    if (!string.IsNullOrEmpty(username))
                    {
                        updateFields.Add("Username = @Username");
                        parameters.Add(new MySqlParameter("@Username", username));
                    }
                    if (!string.IsNullOrEmpty(email))
                    {
                        updateFields.Add("Email = @Email");
                        parameters.Add(new MySqlParameter("@Email", email));
                    }
                    if (!string.IsNullOrEmpty(firstName))
                    {
                        updateFields.Add("FirstName = @FirstName");
                        parameters.Add(new MySqlParameter("@FirstName", firstName));
                    }
                    if (!string.IsNullOrEmpty(middleName))
                    {
                        updateFields.Add("MiddleName = @MiddleName");
                        parameters.Add(new MySqlParameter("@MiddleName", middleName));
                    }
                    if (!string.IsNullOrEmpty(lastName))
                    {
                        updateFields.Add("LastName = @LastName");
                        parameters.Add(new MySqlParameter("@LastName", lastName));
                    }
                    if (!string.IsNullOrEmpty(phoneNumber))
                    {
                        updateFields.Add("PhoneNumber = @PhoneNumber");
                        parameters.Add(new MySqlParameter("@PhoneNumber", phoneNumber));
                    }
                    if (!string.IsNullOrEmpty(password))
                    {
                        string hashedPassword = HashPassword(password);
                        updateFields.Add("PasswordHash = @PasswordHash");
                        parameters.Add(new MySqlParameter("@PasswordHash", hashedPassword));
                    }

                    if (updateFields.Count > 0)
                    {
                        string query = $"UPDATE USERS SET {string.Join(", ", updateFields)} WHERE UserID = @UserID";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddRange(parameters.ToArray());

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // Update session if username was changed
                            if (!string.IsNullOrEmpty(username))
                            {
                                HttpContext.Session.SetString("Username", username);
                            }

                            return Json(new { 
                                success = true, 
                                message = "Profile updated successfully!",
                                newUsername = username // Send back the new username if it was changed
                            });
                        }
                    }

                    return Json(new { success = true, message = "No changes were made." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the profile.");
                return Json(new { success = false, message = "An error occurred while updating the profile: " + ex.Message });
            }
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password), "Password cannot be null or empty.");
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        [HttpGet]
        public IActionResult GetStatistics()
        {
            string userId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "You must be logged in to view statistics." });
            }

            try
            {
                var stats = new
                {
                    totalBookings = 0,
                    activeRequests = 0,
                    totalPosts = 0,
                    visitorPasses = 0
                };

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Get total bookings
                    string bookingsQuery = @"SELECT COUNT(*) FROM facility_reservations 
                                          WHERE UserID = @UserID";
                    MySqlCommand bookingsCmd = new MySqlCommand(bookingsQuery, conn);
                    bookingsCmd.Parameters.AddWithValue("@UserID", userId);
                    stats = new
                    {
                        totalBookings = Convert.ToInt32(bookingsCmd.ExecuteScalar()),
                        activeRequests = 0,
                        totalPosts = 0,
                        visitorPasses = 0
                    };

                    // Get active service requests
                    string requestsQuery = @"SELECT COUNT(*) FROM service_requests 
                                          WHERE UserID = @UserID AND Status IN ('Pending', 'In Progress')";
                    MySqlCommand requestsCmd = new MySqlCommand(requestsQuery, conn);
                    requestsCmd.Parameters.AddWithValue("@UserID", userId);
                    stats = new
                    {
                        stats.totalBookings,
                        activeRequests = Convert.ToInt32(requestsCmd.ExecuteScalar()),
                        stats.totalPosts,
                        stats.visitorPasses
                    };

                    // Get total forum posts
                    string postsQuery = @"SELECT COUNT(*) FROM forum_posts 
                                       WHERE UserID = @UserID";
                    MySqlCommand postsCmd = new MySqlCommand(postsQuery, conn);
                    postsCmd.Parameters.AddWithValue("@UserID", userId);
                    stats = new
                    {
                        stats.totalBookings,
                        stats.activeRequests,
                        totalPosts = Convert.ToInt32(postsCmd.ExecuteScalar()),
                        stats.visitorPasses
                    };

                    // Get active visitor passes
                    string passesQuery = @"SELECT COUNT(*) FROM visitor_passes 
                                        WHERE UserID = @UserID AND Status = 'Active'";
                    MySqlCommand passesCmd = new MySqlCommand(passesQuery, conn);
                    passesCmd.Parameters.AddWithValue("@UserID", userId);
                    stats = new
                    {
                        stats.totalBookings,
                        stats.activeRequests,
                        stats.totalPosts,
                        visitorPasses = Convert.ToInt32(passesCmd.ExecuteScalar())
                    };
                }

                _logger.LogInformation("Retrieved statistics for user {UserId}: {@Stats}", userId, stats);
                return Json(new { success = true, stats = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics for user {UserId}", userId);
                return Json(new { success = false, message = "Error retrieving statistics." });
            }
        }

        [HttpGet]
        public IActionResult GetMonthlyActivity()
        {
            string userId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "You must be logged in to view activity." });
            }

            try
            {
                var months = new List<string>();
                var data = new List<int>();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT 
                                    DATE_FORMAT(ReservationDate, '%Y-%m') as Month,
                                    COUNT(*) as Count
                                  FROM facility_reservations 
                                  WHERE UserID = @UserID
                                  AND ReservationDate >= DATE_SUB(NOW(), INTERVAL 6 MONTH)
                                  GROUP BY DATE_FORMAT(ReservationDate, '%Y-%m')
                                  ORDER BY Month";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            months.Add(DateTime.Parse(reader.GetString("Month") + "-01").ToString("MMM yyyy"));
                            data.Add(reader.GetInt32("Count"));
                        }
                    }
                }

                _logger.LogInformation("Retrieved monthly activity for user {UserId}", userId);
                return Json(new { success = true, months = months, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly activity for user {UserId}", userId);
                return Json(new { success = false, message = "Error retrieving monthly activity." });
            }
        }

        [HttpGet]
        public IActionResult GetServiceRequestStatus()
        {
            string userId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "You must be logged in to view service request status." });
            }

            try
            {
                var labels = new List<string>();
                var data = new List<int>();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT Status, COUNT(*) as Count
                                  FROM service_requests
                                  WHERE UserID = @UserID
                                  GROUP BY Status";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            labels.Add(reader.GetString("Status"));
                            data.Add(reader.GetInt32("Count"));
                        }
                    }
                }

                _logger.LogInformation("Retrieved service request status for user {UserId}", userId);
                return Json(new { success = true, labels = labels, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service request status for user {UserId}", userId);
                return Json(new { success = false, message = "Error retrieving service request status." });
            }
        }

        [HttpGet]
        public IActionResult GetRecentActivities()
        {
            string userId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "You must be logged in to view recent activities." });
            }

            try
            {
                var activities = new List<dynamic>();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT 
                                    'booking' as type,
                                    fr.ReservationID as id,
                                    fr.ReservationDate as timestamp,
                                    CONCAT('Facility: ', f.FacilityName) as title
                                  FROM facility_reservations fr
                                  JOIN facilities f ON fr.FacilityID = f.FacilityID
                                  WHERE fr.UserID = @UserID
                                  
                                  UNION ALL
                                  
                                  SELECT 
                                    'request' as type,
                                    sr.RequestID as id,
                                    sr.RequestDate as timestamp,
                                    CONCAT('Service: ', st.ServiceTypeName) as title
                                  FROM service_requests sr
                                  JOIN service_types st ON sr.ServiceTypeID = st.ServiceTypeID
                                  WHERE sr.UserID = @UserID
                                  
                                  UNION ALL
                                  
                                  SELECT 
                                    'post' as type,
                                    fp.PostID as id,
                                    fp.CreatedAt as timestamp,
                                    CONCAT('Post: ', fp.Title) as title
                                  FROM forum_posts fp
                                  WHERE fp.UserID = @UserID
                                  
                                  UNION ALL
                                  
                                  SELECT 
                                    'visitor' as type,
                                    vp.VisitorPassID as id,
                                    vp.CreatedAt as timestamp,
                                    CONCAT('Visitor: ', vp.VisitorName) as title
                                  FROM visitor_passes vp
                                  WHERE vp.UserID = @UserID
                                  
                                  ORDER BY timestamp DESC
                                  LIMIT 10";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activities.Add(new
                            {
                                type = reader.GetString("type"),
                                id = reader.GetInt32("id"),
                                timestamp = reader.GetDateTime("timestamp"),
                                title = reader.GetString("title")
                            });
                        }
                    }
                }

                _logger.LogInformation("Retrieved recent activities for user {UserId}", userId);
                return Json(new { success = true, activities = activities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent activities for user {UserId}", userId);
                return Json(new { success = false, message = "Error retrieving recent activities." });
            }
        }
    }
}