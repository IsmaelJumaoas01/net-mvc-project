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
                            HttpContext.Session.SetString("UserID", reader["UserID"].ToString());
                            HttpContext.Session.SetString("Username", reader["Username"].ToString());
                            HttpContext.Session.SetString("Role", reader["Role"].ToString());
                            _logger.LogInformation("Login successful for user: {Username}", username);

                            // Redirect based on role
                            string role = reader["Role"].ToString();
                            if (role == "Staff")
                            {
                                return RedirectToAction("StaffDashboard", "Home");
                            }
                            else if (role == "Administrator")
                            {
                                return RedirectToAction("AdminDashboard", "Home");
                            }
                            else
                            {
                                return RedirectToAction("Index", "Home");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Invalid password for user: {Username}", username);
                            TempData["ErrorMessage"] = "Invalid password.";
                        }
                    }
                    else
                    {
                        _logger.LogWarning("User not found: {Username}", username);
                        TempData["ErrorMessage"] = "User not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process for user: {Username}", username);
                TempData["ErrorMessage"] = "An error occurred while logging in. Please try again.";
            }

            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string password, string email, string firstName, string middleName, string lastName, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Password is required.";
                return View();
            }

            string hashedPassword = HashPassword(password);

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
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
            }

            TempData["SuccessMessage"] = "Registration successful. Please log in.";
            return RedirectToAction("Login");
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
                TempData["ErrorMessage"] = "Unauthorized action.";
                return RedirectToAction("ManageUsers");
            }

            if (role == "Staff" && user.Role != "Homeowner")
            {
                TempData["ErrorMessage"] = "Staff can only add Homeowners.";
                return RedirectToAction("ManageUsers");
            }

            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.PasswordHash) || string.IsNullOrWhiteSpace(user.Email))
            {
                TempData["ErrorMessage"] = "Username, Password, and Email are required.";
                return RedirectToAction("ManageUsers");
            }

            string hashedPassword = HashPassword(user.PasswordHash);

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO USERS (Username, PasswordHash, Email, FirstName, MiddleName, LastName, PhoneNumber, Role, CreatedAt) VALUES (@Username, @Password, @Email, @FirstName, @MiddleName, @LastName, @PhoneNumber, @Role, NOW())";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@Password", hashedPassword);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
                cmd.Parameters.AddWithValue("@MiddleName", user.MiddleName);
                cmd.Parameters.AddWithValue("@LastName", user.LastName);
                cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                cmd.Parameters.AddWithValue("@Role", user.Role);

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "User added successfully.";
            return RedirectToAction("ManageUsers");
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
                TempData["ErrorMessage"] = "Unauthorized action.";
                return RedirectToAction("ManageUsers");
            }

            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Email))
            {
                TempData["ErrorMessage"] = "Username and Email are required.";
                return RedirectToAction("ManageUsers");
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE USERS SET Username = @Username, Email = @Email, FirstName = @FirstName, MiddleName = @MiddleName, LastName = @LastName, PhoneNumber = @PhoneNumber, Role = @Role";
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    query += ", PasswordHash = @PasswordHash";
                }
                query += " WHERE UserID = @UserID";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
                cmd.Parameters.AddWithValue("@MiddleName", user.MiddleName);
                cmd.Parameters.AddWithValue("@LastName", user.LastName);
                cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                cmd.Parameters.AddWithValue("@Role", user.Role);
                cmd.Parameters.AddWithValue("@UserID", user.UserID);

                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    string hashedPassword = HashPassword(user.PasswordHash);
                    cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                }

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction("ManageUsers");
        }

        public IActionResult DeleteUser(int id)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                TempData["ErrorMessage"] = "Unauthorized action.";
                return RedirectToAction("ManageUsers");
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM USERS WHERE UserID = @UserID";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserID", id);

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "User deleted successfully.";
            return RedirectToAction("ManageUsers");
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
            TempData["SuccessMessage"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }
    }
}