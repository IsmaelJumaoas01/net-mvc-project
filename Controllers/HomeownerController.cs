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
                TempData["ErrorMessage"] = "You must be logged in to update your profile.";
                return RedirectToAction("Login", "Account");
            }

            if (!string.IsNullOrEmpty(password) && password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return RedirectToAction("Profile");
            }

            _logger.LogInformation("User {UserId} is attempting to update their profile.", userId);
            _logger.LogInformation("New details: Username={Username}, Email={Email}, FirstName={FirstName}, MiddleName={MiddleName}, LastName={LastName}, PhoneNumber={PhoneNumber}", username, email, firstName, middleName, lastName, phoneNumber);

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
                            TempData["ErrorMessage"] = "Username already exists.";
                            return RedirectToAction("Profile");
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
                            TempData["ErrorMessage"] = "Email already exists.";
                            return RedirectToAction("Profile");
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
                            TempData["ErrorMessage"] = "Phone number already exists.";
                            return RedirectToAction("Profile");
                        }
                    }

                    // Update user details
                    var updateFields = new List<string>();
                    var updateParameters = new List<MySqlParameter>();

                    if (!string.IsNullOrEmpty(username))
                    {
                        updateFields.Add("Username = @Username");
                        updateParameters.Add(new MySqlParameter("@Username", username));
                    }
                    if (!string.IsNullOrEmpty(email))
                    {
                        updateFields.Add("Email = @Email");
                        updateParameters.Add(new MySqlParameter("@Email", email));
                    }
                    if (!string.IsNullOrEmpty(firstName))
                    {
                        updateFields.Add("FirstName = @FirstName");
                        updateParameters.Add(new MySqlParameter("@FirstName", firstName));
                    }
                    if (!string.IsNullOrEmpty(middleName))
                    {
                        updateFields.Add("MiddleName = @MiddleName");
                        updateParameters.Add(new MySqlParameter("@MiddleName", middleName));
                    }
                    if (!string.IsNullOrEmpty(lastName))
                    {
                        updateFields.Add("LastName = @LastName");
                        updateParameters.Add(new MySqlParameter("@LastName", lastName));
                    }
                    if (!string.IsNullOrEmpty(phoneNumber))
                    {
                        updateFields.Add("PhoneNumber = @PhoneNumber");
                        updateParameters.Add(new MySqlParameter("@PhoneNumber", phoneNumber));
                    }
                    if (!string.IsNullOrEmpty(password))
                    {
                        string hashedPassword = HashPassword(password);
                        updateFields.Add("PasswordHash = @PasswordHash");
                        updateParameters.Add(new MySqlParameter("@PasswordHash", hashedPassword));
                    }

                    if (updateFields.Count > 0)
                    {
                        string query = $"UPDATE USERS SET {string.Join(", ", updateFields)} WHERE UserID = @UserID";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddRange(updateParameters.ToArray());
                        cmd.ExecuteNonQuery();
                    }
                }

                // Update session data
                if (!string.IsNullOrEmpty(username))
                {
                    HttpContext.Session.SetString("Username", username);
                }

                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction("Profile");
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "An error occurred while updating the profile.");
                TempData["ErrorMessage"] = "An error occurred while updating the profile. Please try again.";
                return RedirectToAction("Profile");
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
    }
}