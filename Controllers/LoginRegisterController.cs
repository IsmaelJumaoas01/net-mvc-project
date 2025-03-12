using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using homeowner.Models;

namespace homeowner.Controllers
{
    public class LoginRegisterController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";
        private readonly ILogger<LoginRegisterController> _logger;

        public LoginRegisterController(ILogger<LoginRegisterController> logger)
        {
            _logger = logger;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            _logger.LogInformation("Login attempt for user: {Username}", username);

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    _logger.LogInformation("Database connection opened successfully.");

                    string query = "SELECT UserID, Username, PasswordHash, Role FROM USERS WHERE Username = @Username";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Username", username);
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        string storedHashedPassword = reader["PasswordHash"]?.ToString() ?? string.Empty;
                        _logger.LogInformation("User found in database. Verifying password...");

                        if (!string.IsNullOrEmpty(storedHashedPassword) && VerifyPassword(password, storedHashedPassword))
                        {
                            // Convert values to string and ensure they're not null
                            string userId = reader["UserID"]?.ToString() ?? "";
                            string userName = reader["Username"]?.ToString() ?? "";
                            string role = reader["Role"]?.ToString() ?? "Homeowner"; // Default to Homeowner if null

                            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userName))
                            {
                                HttpContext.Session.SetString("UserID", userId);
                                HttpContext.Session.SetString("Username", userName);
                                HttpContext.Session.SetString("Role", role);
                                _logger.LogInformation("Login successful for user: {Username}", username);

                                // Redirect based on role
                                string redirectUrl = role switch
                                {
                                    "Staff" => Url.Action("StaffDashboard", "Home"),
                                    "Administrator" => Url.Action("AdminDashboard", "Home"),
                                    _ => Url.Action("Index", "Home")
                                };

                                string welcomeMessage = role switch
                                {
                                    "Staff" => "Login successful. Welcome back! Our dear Staff~.",
                                    "Administrator" => "Login successful. Welcome back! Admin~.",
                                    _ => "Login successful. Welcome back! Our dear User~."
                                };

                                TempData["SuccessMessage"] = welcomeMessage;
                                return Json(new { success = true, redirectUrl });
                            }
                            else
                            {
                                _logger.LogWarning("Invalid user data for user: {Username}", username);
                                return Json(new { success = false, message = "Invalid user data." });
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Invalid password for user: {Username}", username);
                            return Json(new { success = false, message = "Invalid password." });
                        }
                    }
                    else
                    {
                        _logger.LogWarning("User not found: {Username}", username);
                        return Json(new { success = false, message = "User not found." });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process for user: {Username}", username);
                return Json(new { success = false, message = "An error occurred while logging in. Please try again." });
            }
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string password, string confirmPassword, string email, string firstName, string middleName, string lastName, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return Json(new { success = false, message = "Password is required." });
            }

            if (password != confirmPassword)
            {
                return Json(new { success = false, message = "Passwords do not match." });
            }

            if (!IsValidEmail(email))
            {
                return Json(new { success = false, message = "Invalid email format." });
            }

            string hashedPassword = HashPassword(password);

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Check for duplicate username
                    string checkUsernameQuery = "SELECT COUNT(*) FROM USERS WHERE Username = @Username";
                    MySqlCommand checkUsernameCmd = new MySqlCommand(checkUsernameQuery, conn);
                    checkUsernameCmd.Parameters.AddWithValue("@Username", username);
                    long usernameCount = (long)checkUsernameCmd.ExecuteScalar();
                    if (usernameCount > 0)
                    {
                        return Json(new { success = false, message = "Username already exists." });
                    }

                    // Check for duplicate email
                    string checkEmailQuery = "SELECT COUNT(*) FROM USERS WHERE Email = @Email";
                    MySqlCommand checkEmailCmd = new MySqlCommand(checkEmailQuery, conn);
                    checkEmailCmd.Parameters.AddWithValue("@Email", email);
                    long emailCount = (long)checkEmailCmd.ExecuteScalar();
                    if (emailCount > 0)
                    {
                        return Json(new { success = false, message = "Email already exists." });
                    }

                    // Check for duplicate phone number
                    string checkPhoneNumberQuery = "SELECT COUNT(*) FROM USERS WHERE PhoneNumber = @PhoneNumber";
                    MySqlCommand checkPhoneNumberCmd = new MySqlCommand(checkPhoneNumberQuery, conn);
                    checkPhoneNumberCmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                    long phoneNumberCount = (long)checkPhoneNumberCmd.ExecuteScalar();
                    if (phoneNumberCount > 0)
                    {
                        return Json(new { success = false, message = "Phone number already exists." });
                    }

                    // Insert new user
                    string query = "INSERT INTO USERS (Username, PasswordHash, Email, FirstName, MiddleName, LastName, PhoneNumber, Role, CreatedAt) VALUES (@Username, @Password, @Email, @FirstName, @MiddleName, @LastName, @PhoneNumber, 'Homeowner', NOW())";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@MiddleName", middleName);
                    cmd.Parameters.AddWithValue("@LastName", lastName);
                    cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);

                    cmd.ExecuteNonQuery();

                    // Automatically log in the user after registration
                    HttpContext.Session.SetString("UserID", cmd.LastInsertedId.ToString());
                    HttpContext.Session.SetString("Username", username);
                    HttpContext.Session.SetString("Role", "Homeowner");

                    TempData["SuccessMessage"] = "User registered successfully. Welcome!";
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration process for user: {Username}", username);
                return Json(new { success = false, message = "An error occurred while registering. Please try again." });
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

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

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
    }
}