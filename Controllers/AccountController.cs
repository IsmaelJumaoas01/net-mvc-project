using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using homeowner.Models;
using homeowner.ViewModels;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace homeowner.Controllers
{
    public class AccountController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";
        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }


        public IActionResult ManageUsers()
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return RedirectToAction("Index", "Home");
            }

            List<UserModel> users = new List<UserModel>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = role == "Administrator" ? "SELECT * FROM USERS" : "SELECT * FROM USERS WHERE Role = 'Homeowner'";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new UserModel
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
                    });
                }
            }

            var viewModel = new ManageUsersViewModel
            {
                Users = users,
                AddUser = new UserModel(),
                EditUser = new UserModel()
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult AddUser(UserModel user)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized action." });
            }

            if (role == "Staff" && user.Role != "Homeowner")
            {
                return Json(new { success = false, message = "Staff can only add Homeowners." });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(user.Username))
            {
                return Json(new { success = false, message = "Username is required." });
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return Json(new { success = false, message = "Password is required." });
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return Json(new { success = false, message = "Email is required." });
            }

            // Validate username length
            if (user.Username.Length < 5 || user.Username.Length > 50)
            {
                return Json(new { success = false, message = "Username must be between 5 and 50 characters." });
            }

            // Validate email format
            if (!IsValidEmail(user.Email))
            {
                return Json(new { success = false, message = "Invalid email format." });
            }

            // Validate password length and complexity
            if (user.PasswordHash.Length < 8)
            {
                return Json(new { success = false, message = "Password must be at least 8 characters long." });
            }

            // Additional password complexity check
            if (!IsPasswordComplex(user.PasswordHash))
            {
                return Json(new { success = false, message = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character." });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Check for duplicate username
                    string checkUsernameQuery = "SELECT COUNT(*) FROM USERS WHERE Username = @Username";
                    MySqlCommand checkUsernameCmd = new MySqlCommand(checkUsernameQuery, conn);
                    checkUsernameCmd.Parameters.AddWithValue("@Username", user.Username);
                    long usernameCount = (long)checkUsernameCmd.ExecuteScalar();
                    if (usernameCount > 0)
                    {
                        return Json(new { success = false, message = "Username already exists." });
                    }

                    // Check for duplicate email
                    string checkEmailQuery = "SELECT COUNT(*) FROM USERS WHERE Email = @Email";
                    MySqlCommand checkEmailCmd = new MySqlCommand(checkEmailQuery, conn);
                    checkEmailCmd.Parameters.AddWithValue("@Email", user.Email);
                    long emailCount = (long)checkEmailCmd.ExecuteScalar();
                    if (emailCount > 0)
                    {
                        return Json(new { success = false, message = "Email already exists." });
                    }

                    // Check for duplicate phone number if provided
                    if (!string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        if (!IsValidPhoneNumber(user.PhoneNumber))
                        {
                            return Json(new { success = false, message = "Invalid phone number format." });
                        }

                        string checkPhoneQuery = "SELECT COUNT(*) FROM USERS WHERE PhoneNumber = @PhoneNumber";
                        MySqlCommand checkPhoneCmd = new MySqlCommand(checkPhoneQuery, conn);
                        checkPhoneCmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                        long phoneCount = (long)checkPhoneCmd.ExecuteScalar();
                        if (phoneCount > 0)
                        {
                            return Json(new { success = false, message = "Phone number already exists." });
                        }
                    }

                    string hashedPassword = HashPassword(user.PasswordHash);

                    string query = @"INSERT INTO USERS 
                        (Username, PasswordHash, Email, FirstName, MiddleName, LastName, PhoneNumber, Role, CreatedAt) 
                        VALUES (@Username, @Password, @Email, @FirstName, @MiddleName, @LastName, @PhoneNumber, @Role, NOW())";
                    
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Username", user.Username);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@FirstName", user.FirstName ?? "");
                    cmd.Parameters.AddWithValue("@MiddleName", user.MiddleName ?? "");
                    cmd.Parameters.AddWithValue("@LastName", user.LastName ?? "");
                    cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber ?? "");
                    cmd.Parameters.AddWithValue("@Role", user.Role);

                    cmd.ExecuteNonQuery();

                    var newUser = new
                    {
                        userID = cmd.LastInsertedId,
                        username = user.Username,
                        email = user.Email,
                        firstName = user.FirstName,
                        middleName = user.MiddleName,
                        lastName = user.LastName,
                        phoneNumber = user.PhoneNumber,
                        role = user.Role,
                        createdAt = DateTime.Now
                    };

                    return Json(new { 
                        success = true, 
                        message = "User added successfully.",
                        user = newUser
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user");
                return Json(new { success = false, message = "An error occurred while adding the user." });
            }
        }

        [HttpGet]
        public IActionResult GetUser(int id)
        {
            UserModel user = null;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM USERS WHERE UserID = @UserID";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserID", id);
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
                return NotFound();
            }

            return Json(new
            {
                userID = user.UserID,
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                middleName = user.MiddleName,
                lastName = user.LastName,
                phoneNumber = user.PhoneNumber,
                role = user.Role
            });
        }

        [HttpPost]
        public IActionResult EditUser(UserModel user)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized action." });
            }

            if (string.IsNullOrWhiteSpace(user.Username))
            {
                return Json(new { success = false, message = "Username is required." });
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return Json(new { success = false, message = "Email is required." });
            }

            // Validate username length
            if (user.Username.Length < 5 || user.Username.Length > 50)
            {
                return Json(new { success = false, message = "Username must be between 5 and 50 characters." });
            }

            // Validate email format
            if (!IsValidEmail(user.Email))
            {
                return Json(new { success = false, message = "Invalid email format." });
            }

            // Validate password if provided
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                if (user.PasswordHash.Length < 8)
                {
                    return Json(new { success = false, message = "Password must be at least 8 characters long." });
                }

                if (!IsPasswordComplex(user.PasswordHash))
                {
                    return Json(new { success = false, message = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character." });
                }
            }

            // Validate phone number if provided
            if (!string.IsNullOrEmpty(user.PhoneNumber) && !IsValidPhoneNumber(user.PhoneNumber))
            {
                return Json(new { success = false, message = "Invalid phone number format." });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Check for duplicate username
                    string checkUsernameQuery = "SELECT COUNT(*) FROM USERS WHERE Username = @Username AND UserID != @UserID";
                    MySqlCommand checkUsernameCmd = new MySqlCommand(checkUsernameQuery, conn);
                    checkUsernameCmd.Parameters.AddWithValue("@Username", user.Username);
                    checkUsernameCmd.Parameters.AddWithValue("@UserID", user.UserID);
                    long usernameCount = (long)checkUsernameCmd.ExecuteScalar();
                    if (usernameCount > 0)
                    {
                        return Json(new { success = false, message = "Username already exists." });
                    }

                    // Check for duplicate email
                    string checkEmailQuery = "SELECT COUNT(*) FROM USERS WHERE Email = @Email AND UserID != @UserID";
                    MySqlCommand checkEmailCmd = new MySqlCommand(checkEmailQuery, conn);
                    checkEmailCmd.Parameters.AddWithValue("@Email", user.Email);
                    checkEmailCmd.Parameters.AddWithValue("@UserID", user.UserID);
                    long emailCount = (long)checkEmailCmd.ExecuteScalar();
                    if (emailCount > 0)
                    {
                        return Json(new { success = false, message = "Email already exists." });
                    }

                    // Check for duplicate phone number if provided
                    if (!string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        string checkPhoneQuery = "SELECT COUNT(*) FROM USERS WHERE PhoneNumber = @PhoneNumber AND UserID != @UserID";
                        MySqlCommand checkPhoneCmd = new MySqlCommand(checkPhoneQuery, conn);
                        checkPhoneCmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                        checkPhoneCmd.Parameters.AddWithValue("@UserID", user.UserID);
                        long phoneCount = (long)checkPhoneCmd.ExecuteScalar();
                        if (phoneCount > 0)
                        {
                            return Json(new { success = false, message = "Phone number already exists." });
                        }
                    }

                    string query = "UPDATE USERS SET Username = @Username, Email = @Email, FirstName = @FirstName, " +
                                  "MiddleName = @MiddleName, LastName = @LastName, PhoneNumber = @PhoneNumber, Role = @Role";
                                
                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        query += ", PasswordHash = @PasswordHash";
                    }
                    query += " WHERE UserID = @UserID";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Username", user.Username);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@FirstName", user.FirstName ?? "");
                    cmd.Parameters.AddWithValue("@MiddleName", user.MiddleName ?? "");
                    cmd.Parameters.AddWithValue("@LastName", user.LastName ?? "");
                    cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber ?? "");
                    cmd.Parameters.AddWithValue("@Role", user.Role);
                    cmd.Parameters.AddWithValue("@UserID", user.UserID);

                    if (!string.IsNullOrEmpty(user.PasswordHash))
                    {
                        string hashedPassword = HashPassword(user.PasswordHash);
                        cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    }

                    cmd.ExecuteNonQuery();

                    var updatedUser = new
                    {
                        userID = user.UserID,
                        username = user.Username,
                        email = user.Email,
                        firstName = user.FirstName,
                        middleName = user.MiddleName,
                        lastName = user.LastName,
                        phoneNumber = user.PhoneNumber,
                        role = user.Role
                    };

                    return Json(new { 
                        success = true, 
                        message = "User updated successfully.",
                        user = updatedUser
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return Json(new { success = false, message = "An error occurred while updating the user." });
            }
        }

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized action." });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM USERS WHERE UserID = @UserID";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UserID", id);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "User deleted successfully." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "User not found." });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return Json(new { success = false, message = "An error occurred while deleting the user." });
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

        private bool VerifyPassword(string inputPassword, string storedHashedPassword)
        {
            return HashPassword(inputPassword) == storedHashedPassword;
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            TempData["SuccessMessage"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Profile()
        {
            string userId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "You must be logged in to view your profile.";
                return RedirectToAction("Index", "Home");
            }

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
                return RedirectToAction("Index", "Home");
            }

            return View(user);
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsPasswordComplex(string password)
        {
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(c => !char.IsLetterOrDigit(c));
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            // This is a basic validation that checks for a minimum of 10 digits
            // You can modify this regex based on your specific phone number format requirements
            return Regex.IsMatch(phoneNumber, @"^\+?[\d\s-]{10,}$");
        }
    }
}