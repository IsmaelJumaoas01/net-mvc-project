using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System.Collections.Generic;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;

namespace homeowner.Controllers
{
    public class SecurityController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";

        // GET: Security/Vehicles
        public IActionResult Vehicles()
        {
            string role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Index", "Home");
            }

            List<VehicleRegistration> vehicles = new List<VehicleRegistration>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT VehicleId, UserID, PlateNumber, VehicleModel, VehicleColor, VehicleType, 
                                        VehicleBrand, RegistrationDate, ExpiryDate, Status, State, Notes, RejectionReason 
                                 FROM VehicleRegistrations 
                                 WHERE UserID = @UserID 
                                 ORDER BY RegistrationDate DESC";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@UserID", HttpContext.Session.GetString("UserID"));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var vehicle = new VehicleRegistration
                            {
                                VehicleId = reader.GetInt32("VehicleId"),
                                UserID = reader.GetInt32("UserID"),
                                PlateNumber = reader.GetString("PlateNumber"),
                                VehicleModel = reader.GetString("VehicleModel"),
                                VehicleColor = reader.GetString("VehicleColor"),
                                VehicleType = reader.GetString("VehicleType"),
                                VehicleBrand = reader.GetString("VehicleBrand"),
                                RegistrationDate = reader.GetDateTime("RegistrationDate"),
                                ExpiryDate = reader.GetDateTime("ExpiryDate"),
                                Status = reader.GetString("Status"),
                                State = reader.IsDBNull(reader.GetOrdinal("State")) ? "Pending" : reader.GetString("State"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString("Notes")
                            };

                            // Only try to get RejectionReason if State is Rejected
                            if (vehicle.State == "Rejected" && !reader.IsDBNull(reader.GetOrdinal("RejectionReason")))
                            {
                                vehicle.RejectionReason = reader.GetString("RejectionReason");
                            }

                            vehicles.Add(vehicle);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading vehicle registrations. Please try again later.";
                Console.WriteLine($"Error in Vehicles action: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            ViewBag.IsAdmin = role == "Administrator";
            return View(vehicles);
        }

        // GET: Security/RegisterVehicle
        public IActionResult RegisterVehicle()
        {
            string role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Security/RegisterVehicle
        [HttpPost]
        public IActionResult RegisterVehicle([FromBody] VehicleRegistration vehicle)
        {
            // Manual validation since we're receiving JSON
            if (vehicle == null)
            {
                return Json(new { success = false, message = "No data received" });
            }

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(vehicle.PlateNumber))
                errors.Add("Plate number is required");
            else if (vehicle.PlateNumber.Length > 20)
                errors.Add("Plate number cannot be longer than 20 characters");

            if (string.IsNullOrWhiteSpace(vehicle.VehicleType))
                errors.Add("Vehicle type is required");
            else if (vehicle.VehicleType.Length > 20)
                errors.Add("Vehicle type cannot be longer than 20 characters");

            if (string.IsNullOrWhiteSpace(vehicle.VehicleBrand))
                errors.Add("Vehicle brand is required");
            else if (vehicle.VehicleBrand.Length > 50)
                errors.Add("Vehicle brand cannot be longer than 50 characters");

            if (string.IsNullOrWhiteSpace(vehicle.VehicleModel))
                errors.Add("Vehicle model is required");
            else if (vehicle.VehicleModel.Length > 50)
                errors.Add("Vehicle model cannot be longer than 50 characters");

            if (string.IsNullOrWhiteSpace(vehicle.VehicleColor))
                errors.Add("Vehicle color is required");
            else if (vehicle.VehicleColor.Length > 20)
                errors.Add("Vehicle color cannot be longer than 20 characters");

            if (vehicle.ExpiryDate == DateTime.MinValue)
                errors.Add("Expiry date is required");
            else if (vehicle.ExpiryDate < DateTime.Today)
                errors.Add("Expiry date must be today or in the future");

            if (errors.Any())
            {
                return Json(new { success = false, message = "Validation failed", errors = errors });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO VehicleRegistrations 
                                 (UserID, PlateNumber, VehicleModel, VehicleColor, VehicleType, VehicleBrand, 
                                  RegistrationDate, ExpiryDate, Status, State, Notes) 
                                 VALUES (@UserID, @PlateNumber, @VehicleModel, @VehicleColor, @VehicleType, @VehicleBrand, 
                                         @RegistrationDate, @ExpiryDate, @Status, 'Pending', @Notes)";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    
                    // Convert UserID from session to integer
                    if (!int.TryParse(HttpContext.Session.GetString("UserID"), out int userId))
                    {
                        return Json(new { success = false, message = "Invalid user session. Please log in again." });
                    }

                    // Determine status based on expiry date
                    string status;
                    if (vehicle.ExpiryDate < DateTime.Today)
                    {
                        status = "Expired";
                    }
                    else
                    {
                        status = "Active";
                    }
                    
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@PlateNumber", vehicle.PlateNumber.Trim());
                    cmd.Parameters.AddWithValue("@VehicleModel", vehicle.VehicleModel.Trim());
                    cmd.Parameters.AddWithValue("@VehicleColor", vehicle.VehicleColor.Trim());
                    cmd.Parameters.AddWithValue("@VehicleType", vehicle.VehicleType.Trim());
                    cmd.Parameters.AddWithValue("@VehicleBrand", vehicle.VehicleBrand.Trim());
                    cmd.Parameters.AddWithValue("@RegistrationDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@ExpiryDate", vehicle.ExpiryDate);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(vehicle.Notes) ? (object)DBNull.Value : vehicle.Notes.Trim());
                    cmd.ExecuteNonQuery();

                    return Json(new { success = true, message = $"Vehicle registration submitted successfully! Status: {status}" });
                }
            }
            catch (Exception ex)
            {
                // Log the exception details
                Console.WriteLine($"Error in RegisterVehicle: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "An error occurred while registering the vehicle. Please try again." });
            }
        }

        // GET: Security/ManageVehicles (for staff/admin)
        public IActionResult ManageVehicles()
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return RedirectToAction("Index", "Home");
            }

            List<VehicleRegistration> vehicles = new List<VehicleRegistration>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT v.*, u.Username 
                             FROM VehicleRegistrations v 
                             JOIN USERS u ON v.UserID = u.UserID 
                             ORDER BY RegistrationDate DESC";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var vehicle = new VehicleRegistration
                        {
                            VehicleId = reader.GetInt32("VehicleId"),
                            UserID = reader.GetInt32("UserID"),
                            PlateNumber = reader.GetString("PlateNumber"),
                            VehicleModel = reader.GetString("VehicleModel"),
                            VehicleColor = reader.GetString("VehicleColor"),
                            VehicleType = reader.GetString("VehicleType"),
                            VehicleBrand = reader.GetString("VehicleBrand"),
                            RegistrationDate = reader.GetDateTime("RegistrationDate"),
                            ExpiryDate = reader.GetDateTime("ExpiryDate"),
                            Status = reader.GetString("Status"),
                            Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString("Notes"),
                            Username = reader.GetString("Username"),
                            State = reader.IsDBNull(reader.GetOrdinal("State")) ? "Pending" : reader.GetString("State")
                        };

                        // Only try to get RejectionReason if State is Rejected
                        if (vehicle.State == "Rejected" && !reader.IsDBNull(reader.GetOrdinal("RejectionReason")))
                        {
                            vehicle.RejectionReason = reader.GetString("RejectionReason");
                        }

                        vehicles.Add(vehicle);
                    }
                }
            }

            return View(vehicles);
        }

        // POST: Security/ApproveVehicle
        [HttpPost]
        public IActionResult ApproveVehicle(int id)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // First check if the vehicle exists and its current state
                    string checkSql = "SELECT State FROM VehicleRegistrations WHERE VehicleId = @VehicleId";
                    MySqlCommand checkCmd = new MySqlCommand(checkSql, conn);
                    checkCmd.Parameters.AddWithValue("@VehicleId", id);
                    
                    var currentState = checkCmd.ExecuteScalar()?.ToString();
                    if (currentState == null)
                    {
                        return Json(new { success = false, message = "Vehicle registration not found" });
                    }
                    if (currentState == "Accepted")
                    {
                        return Json(new { success = false, message = "Vehicle registration is already approved" });
                    }

                    string sql = @"UPDATE VehicleRegistrations 
                                 SET State = 'Accepted', 
                                     Status = 'Active',
                                     RejectionReason = NULL
                                 WHERE VehicleId = @VehicleId";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@VehicleId", id);
                    
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        return Json(new { success = false, message = "Failed to update vehicle registration" });
                    }

                    return Json(new { success = true, message = "Vehicle registration approved successfully" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ApproveVehicle: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "An error occurred while approving the vehicle registration. Please try again." });
            }
        }

        // POST: Security/RejectVehicle
        [HttpPost]
        public IActionResult RejectVehicle(int id, string reason)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return Json(new { success = false, message = "Rejection reason is required" });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // First check if the vehicle exists and its current state
                    string checkSql = "SELECT State FROM VehicleRegistrations WHERE VehicleId = @VehicleId";
                    MySqlCommand checkCmd = new MySqlCommand(checkSql, conn);
                    checkCmd.Parameters.AddWithValue("@VehicleId", id);
                    
                    var currentState = checkCmd.ExecuteScalar()?.ToString();
                    if (currentState == null)
                    {
                        return Json(new { success = false, message = "Vehicle registration not found" });
                    }
                    if (currentState == "Rejected")
                    {
                        return Json(new { success = false, message = "Vehicle registration is already rejected" });
                    }

                    string sql = @"UPDATE VehicleRegistrations 
                                 SET State = 'Rejected', 
                                     Status = 'Inactive',
                                     RejectionReason = @RejectionReason
                                 WHERE VehicleId = @VehicleId";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@VehicleId", id);
                    cmd.Parameters.AddWithValue("@RejectionReason", reason);
                    
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        return Json(new { success = false, message = "Failed to update vehicle registration" });
                    }

                    return Json(new { success = true, message = "Vehicle registration rejected successfully" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RejectVehicle: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "An error occurred while rejecting the vehicle registration. Please try again." });
            }
        }

        // GET: Security/VisitorPasses
        public IActionResult VisitorPasses()
        {
            string role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Index", "Home");
            }

            List<VisitorPass> passes = new List<VisitorPass>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT * FROM VisitorPasses WHERE UserID = @UserID ORDER BY CreatedAt DESC";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    
                    // Convert UserID from session to integer
                    if (!int.TryParse(HttpContext.Session.GetString("UserID"), out int userId))
                    {
                        TempData["Error"] = "Invalid user session. Please log in again.";
                        return RedirectToAction("Index", "Home");
                    }
                    
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            passes.Add(new VisitorPass
                            {
                                VisitorPassId = reader.GetInt32("VisitorPassId"),
                                UserID = reader.GetInt32("UserID"),
                                VisitorName = reader.GetString("VisitorName"),
                                VisitorContact = reader.GetString("VisitorContact"),
                                Purpose = reader.GetString("Purpose"),
                                VisitDate = reader.GetDateTime("VisitDate"),
                                VisitTime = reader.GetTimeSpan("VisitTime"),
                                ExpiryDate = reader.GetDateTime("ExpiryDate"),
                                Status = reader.GetString("Status"),
                                VehiclePlateNumber = reader.IsDBNull(reader.GetOrdinal("VehiclePlateNumber")) ? null : reader.GetString("VehiclePlateNumber"),
                                VehicleModel = reader.IsDBNull(reader.GetOrdinal("VehicleModel")) ? null : reader.GetString("VehicleModel"),
                                VehicleColor = reader.IsDBNull(reader.GetOrdinal("VehicleColor")) ? null : reader.GetString("VehicleColor"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                ApprovedAt = reader.IsDBNull(reader.GetOrdinal("ApprovedAt")) ? null : (DateTime?)reader.GetDateTime("ApprovedAt"),
                                ApprovedBy = reader.IsDBNull(reader.GetOrdinal("ApprovedBy")) ? null : (int?)reader.GetInt32("ApprovedBy"),
                                RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason")) ? null : reader.GetString("RejectionReason")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading visitor passes.";
            }

            ViewBag.IsAdmin = role == "Administrator";
            return View(passes);
        }

        // GET: Security/RequestVisitorPass
        public IActionResult RequestVisitorPass()
        {
            string role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Security/RequestVisitorPass
        [HttpPost]
        public IActionResult RequestVisitorPass([FromBody] VisitorPass pass)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = "Invalid form data. Please check all required fields.", errors = errors });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO VisitorPasses 
                                 (UserID, VisitorName, VisitorContact, Purpose, VisitDate, VisitTime, ExpiryDate, 
                                  VehiclePlateNumber, VehicleModel, VehicleColor, CreatedAt, Status) 
                                 VALUES (@UserID, @VisitorName, @VisitorContact, @Purpose, @VisitDate, @VisitTime, @ExpiryDate, 
                                         @VehiclePlateNumber, @VehicleModel, @VehicleColor, @CreatedAt, 'Pending')";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    
                    // Convert UserID from session to integer
                    if (!int.TryParse(HttpContext.Session.GetString("UserID"), out int userId))
                    {
                        return Json(new { success = false, message = "Invalid user session. Please log in again." });
                    }
                    
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@VisitorName", pass.VisitorName);
                    cmd.Parameters.AddWithValue("@VisitorContact", pass.VisitorContact);
                    cmd.Parameters.AddWithValue("@Purpose", pass.Purpose);
                    cmd.Parameters.AddWithValue("@VisitDate", pass.VisitDate);
                    cmd.Parameters.AddWithValue("@VisitTime", pass.VisitTime);
                    cmd.Parameters.AddWithValue("@ExpiryDate", pass.ExpiryDate);
                    cmd.Parameters.AddWithValue("@VehiclePlateNumber", pass.VehiclePlateNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@VehicleModel", pass.VehicleModel ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@VehicleColor", pass.VehicleColor ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    cmd.ExecuteNonQuery();

                    return Json(new { success = true, message = "Visitor pass request submitted successfully!" });
                }
            }
            catch (Exception ex)
            {
                // Log the exception details
                Console.WriteLine($"Error in RequestVisitorPass: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "An error occurred while requesting the visitor pass. Please try again." });
            }
        }

        // GET: Security/ManageVisitorPasses (for staff/admin)
        public IActionResult ManageVisitorPasses()
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return RedirectToAction("Index", "Home");
            }

            List<VisitorPass> passes = new List<VisitorPass>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT v.*, u.Username FROM VisitorPasses v JOIN USERS u ON v.UserID = u.UserID ORDER BY CreatedAt DESC";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        passes.Add(new VisitorPass
                        {
                            VisitorPassId = reader.GetInt32("VisitorPassId"),
                            UserID = reader.GetInt32("UserID"),
                            VisitorName = reader.GetString("VisitorName"),
                            VisitorContact = reader.GetString("VisitorContact"),
                            Purpose = reader.GetString("Purpose"),
                            VisitDate = reader.GetDateTime("VisitDate"),
                            VisitTime = reader.GetTimeSpan("VisitTime"),
                            ExpiryDate = reader.GetDateTime("ExpiryDate"),
                            Status = reader.GetString("Status"),
                            VehiclePlateNumber = reader.IsDBNull(reader.GetOrdinal("VehiclePlateNumber")) ? null : reader.GetString("VehiclePlateNumber"),
                            VehicleModel = reader.IsDBNull(reader.GetOrdinal("VehicleModel")) ? null : reader.GetString("VehicleModel"),
                            VehicleColor = reader.IsDBNull(reader.GetOrdinal("VehicleColor")) ? null : reader.GetString("VehicleColor"),
                            CreatedAt = reader.GetDateTime("CreatedAt"),
                            ApprovedAt = reader.IsDBNull(reader.GetOrdinal("ApprovedAt")) ? null : (DateTime?)reader.GetDateTime("ApprovedAt"),
                            ApprovedBy = reader.IsDBNull(reader.GetOrdinal("ApprovedBy")) ? null : (int?)reader.GetInt32("ApprovedBy"),
                            RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason")) ? null : reader.GetString("RejectionReason"),
                            Username = reader.GetString("Username")
                        });
                    }
                }
            }

            return View(passes);
        }

        // POST: Security/ApproveVisitorPass
        [HttpPost]
        public IActionResult ApproveVisitorPass(int id)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"UPDATE VisitorPasses 
                                 SET Status = 'Approved', 
                                     ApprovedAt = @ApprovedAt, 
                                     ApprovedBy = @ApprovedBy 
                                 WHERE VisitorPassId = @VisitorPassId";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    
                    // Convert UserID from session to integer
                    if (!int.TryParse(HttpContext.Session.GetString("UserID"), out int userId))
                    {
                        return Json(new { success = false, message = "Invalid user session. Please log in again." });
                    }
                    
                    cmd.Parameters.AddWithValue("@VisitorPassId", id);
                    cmd.Parameters.AddWithValue("@ApprovedAt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@ApprovedBy", userId);
                    cmd.ExecuteNonQuery();

                    return Json(new { success = true, message = "Visitor pass approved successfully" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while approving the visitor pass" });
            }
        }

        // POST: Security/RejectVisitorPass
        [HttpPost]
        public IActionResult RejectVisitorPass(int id, string reason)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"UPDATE VisitorPasses 
                                 SET Status = 'Rejected', 
                                     RejectionReason = @RejectionReason 
                                 WHERE VisitorPassId = @VisitorPassId";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@VisitorPassId", id);
                    cmd.Parameters.AddWithValue("@RejectionReason", reason);
                    cmd.ExecuteNonQuery();

                    return Json(new { success = true, message = "Visitor pass rejected successfully" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while rejecting the visitor pass" });
            }
        }

        // GET: Security/GenerateVisitorPass
        public IActionResult GenerateVisitorPass(int id)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT v.*, u.Username 
                                 FROM VisitorPasses v 
                                 JOIN USERS u ON v.UserID = u.UserID 
                                 WHERE v.VisitorPassId = @VisitorPassId";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@VisitorPassId", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Create a new PDF document
                            using (MemoryStream ms = new MemoryStream())
                            {
                                Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                                document.Open();

                                // Add title
                                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20);
                                Paragraph title = new Paragraph("VISITOR PASS", titleFont);
                                title.Alignment = Element.ALIGN_CENTER;
                                title.SpacingAfter = 20f;
                                document.Add(title);

                                // Add QR Code with pass ID for verification
                                BarcodeQRCode qrCode = new BarcodeQRCode($"VisitorPass-{id}", 100, 100, null);
                                Image qrCodeImage = qrCode.GetImage();
                                qrCodeImage.Alignment = Element.ALIGN_RIGHT;
                                document.Add(qrCodeImage);

                                // Add visitor information
                                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                                Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                                // Add pass details in a table format
                                PdfPTable table = new PdfPTable(2);
                                table.WidthPercentage = 100;
                                table.SpacingBefore = 20f;
                                table.SpacingAfter = 20f;

                                // Set column widths
                                float[] widths = new float[] { 1f, 2f };
                                table.SetWidths(widths);

                                // Add rows
                                AddTableRow(table, "Pass ID:", id.ToString(), headerFont, normalFont);
                                AddTableRow(table, "Visitor Name:", reader.GetString("VisitorName"), headerFont, normalFont);
                                AddTableRow(table, "Contact:", reader.GetString("VisitorContact"), headerFont, normalFont);
                                AddTableRow(table, "Purpose:", reader.GetString("Purpose"), headerFont, normalFont);
                                AddTableRow(table, "Visit Date:", reader.GetDateTime("VisitDate").ToString("MMM dd, yyyy"), headerFont, normalFont);
                                AddTableRow(table, "Visit Time:", reader.GetTimeSpan("VisitTime").ToString(@"hh\:mm"), headerFont, normalFont);
                                AddTableRow(table, "Expiry Date:", reader.GetDateTime("ExpiryDate").ToString("MMM dd, yyyy"), headerFont, normalFont);
                                AddTableRow(table, "Status:", reader.GetString("Status"), headerFont, normalFont);
                                AddTableRow(table, "Homeowner:", reader.GetString("Username"), headerFont, normalFont);

                                // Add vehicle information if available
                                if (!reader.IsDBNull(reader.GetOrdinal("VehiclePlateNumber")))
                                {
                                    table.SpacingBefore = 10f;
                                    AddTableRow(table, "Vehicle Plate:", reader.GetString("VehiclePlateNumber"), headerFont, normalFont);
                                    AddTableRow(table, "Vehicle Model:", reader.GetString("VehicleModel"), headerFont, normalFont);
                                    AddTableRow(table, "Vehicle Color:", reader.GetString("VehicleColor"), headerFont, normalFont);
                                }

                                document.Add(table);

                                // Add footer with approval details
                                Paragraph footer = new Paragraph();
                                footer.Alignment = Element.ALIGN_CENTER;
                                footer.Add(new Chunk($"Approved on: {reader.GetDateTime("ApprovedAt").ToString("MMM dd, yyyy HH:mm")}\n", normalFont));
                                footer.Add(new Chunk("This is an official visitor pass. Please present this to the security personnel.", normalFont));
                                document.Add(footer);

                                document.Close();

                                // Return the PDF file
                                byte[] fileBytes = ms.ToArray();
                                string fileName = $"VisitorPass_{id}.pdf";
                                return File(fileBytes, "application/pdf", fileName);
                            }
                        }
                    }
                }

                return NotFound("Visitor pass not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while generating the visitor pass.");
            }
        }

        private void AddTableRow(PdfPTable table, string label, string value, Font headerFont, Font valueFont)
        {
            PdfPCell labelCell = new PdfPCell(new Phrase(label, headerFont));
            labelCell.Border = Rectangle.BOTTOM_BORDER;
            labelCell.PaddingBottom = 8f;
            labelCell.BorderColor = BaseColor.LIGHT_GRAY;

            PdfPCell valueCell = new PdfPCell(new Phrase(value, valueFont));
            valueCell.Border = Rectangle.BOTTOM_BORDER;
            valueCell.PaddingBottom = 8f;
            valueCell.BorderColor = BaseColor.LIGHT_GRAY;

            table.AddCell(labelCell);
            table.AddCell(valueCell);
        }

        // GET: Security/GetVisitorPassDetails
        public IActionResult GetVisitorPassDetails(int id)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT v.*, u.Username 
                                 FROM VisitorPasses v 
                                 JOIN USERS u ON v.UserID = u.UserID 
                                 WHERE v.VisitorPassId = @VisitorPassId";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@VisitorPassId", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var pass = new VisitorPass
                            {
                                VisitorPassId = reader.GetInt32("VisitorPassId"),
                                UserID = reader.GetInt32("UserID"),
                                VisitorName = reader.GetString("VisitorName"),
                                VisitorContact = reader.GetString("VisitorContact"),
                                Purpose = reader.GetString("Purpose"),
                                VisitDate = reader.GetDateTime("VisitDate"),
                                VisitTime = reader.GetTimeSpan("VisitTime"),
                                ExpiryDate = reader.GetDateTime("ExpiryDate"),
                                Status = reader.GetString("Status"),
                                VehiclePlateNumber = reader.IsDBNull(reader.GetOrdinal("VehiclePlateNumber")) ? null : reader.GetString("VehiclePlateNumber"),
                                VehicleModel = reader.IsDBNull(reader.GetOrdinal("VehicleModel")) ? null : reader.GetString("VehicleModel"),
                                VehicleColor = reader.IsDBNull(reader.GetOrdinal("VehicleColor")) ? null : reader.GetString("VehicleColor"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                ApprovedAt = reader.IsDBNull(reader.GetOrdinal("ApprovedAt")) ? null : (DateTime?)reader.GetDateTime("ApprovedAt"),
                                ApprovedBy = reader.IsDBNull(reader.GetOrdinal("ApprovedBy")) ? null : (int?)reader.GetInt32("ApprovedBy"),
                                RejectionReason = reader.IsDBNull(reader.GetOrdinal("RejectionReason")) ? null : reader.GetString("RejectionReason"),
                                Username = reader.GetString("Username")
                            };

                            return PartialView("_VisitorPassDetails", pass);
                        }
                    }
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving visitor pass details.");
            }
        }

        // GET: Security/EditVisitorPass
        public IActionResult EditVisitorPass(int id)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT * FROM VisitorPasses WHERE VisitorPassId = @VisitorPassId";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@VisitorPassId", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var pass = new VisitorPass
                            {
                                VisitorPassId = reader.GetInt32("VisitorPassId"),
                                UserID = reader.GetInt32("UserID"),
                                VisitorName = reader.GetString("VisitorName"),
                                VisitorContact = reader.GetString("VisitorContact"),
                                Purpose = reader.GetString("Purpose"),
                                VisitDate = reader.GetDateTime("VisitDate"),
                                VisitTime = reader.GetTimeSpan("VisitTime"),
                                ExpiryDate = reader.GetDateTime("ExpiryDate"),
                                Status = reader.GetString("Status"),
                                VehiclePlateNumber = reader.IsDBNull(reader.GetOrdinal("VehiclePlateNumber")) ? null : reader.GetString("VehiclePlateNumber"),
                                VehicleModel = reader.IsDBNull(reader.GetOrdinal("VehicleModel")) ? null : reader.GetString("VehicleModel"),
                                VehicleColor = reader.IsDBNull(reader.GetOrdinal("VehicleColor")) ? null : reader.GetString("VehicleColor")
                            };

                            return Json(new { success = true, data = pass });
                        }
                    }
                }

                return Json(new { success = false, message = "Visitor pass not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while retrieving the visitor pass" });
            }
        }

        // POST: Security/UpdateVisitorPass
        [HttpPost]
        public IActionResult UpdateVisitorPass([FromBody] VisitorPass pass)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid form data" });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"UPDATE VisitorPasses 
                                 SET VisitorName = @VisitorName,
                                     VisitorContact = @VisitorContact,
                                     Purpose = @Purpose,
                                     VisitDate = @VisitDate,
                                     VisitTime = @VisitTime,
                                     ExpiryDate = @ExpiryDate,
                                     VehiclePlateNumber = @VehiclePlateNumber,
                                     VehicleModel = @VehicleModel,
                                     VehicleColor = @VehicleColor
                                 WHERE VisitorPassId = @VisitorPassId 
                                 AND UserID = @UserID 
                                 AND Status = 'Approved'";

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@VisitorPassId", pass.VisitorPassId);
                    cmd.Parameters.AddWithValue("@UserID", pass.UserID);
                    cmd.Parameters.AddWithValue("@VisitorName", pass.VisitorName);
                    cmd.Parameters.AddWithValue("@VisitorContact", pass.VisitorContact);
                    cmd.Parameters.AddWithValue("@Purpose", pass.Purpose);
                    cmd.Parameters.AddWithValue("@VisitDate", pass.VisitDate);
                    cmd.Parameters.AddWithValue("@VisitTime", pass.VisitTime);
                    cmd.Parameters.AddWithValue("@ExpiryDate", pass.ExpiryDate);
                    cmd.Parameters.AddWithValue("@VehiclePlateNumber", pass.VehiclePlateNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@VehicleModel", pass.VehicleModel ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@VehicleColor", pass.VehicleColor ?? (object)DBNull.Value);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "Visitor pass updated successfully" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Failed to update visitor pass" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while updating the visitor pass" });
            }
        }

        // POST: Security/DeleteVisitorPass
        [HttpPost]
        public IActionResult DeleteVisitorPass(int id)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM VisitorPasses WHERE VisitorPassId = @VisitorPassId";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@VisitorPassId", id);
                    
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "Visitor pass deleted successfully" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Visitor pass not found" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while deleting the visitor pass" });
            }
        }

        // POST: Security/DeleteVehicle
        [HttpPost]
        public IActionResult DeleteVehicle(int id)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM VehicleRegistrations WHERE VehicleId = @VehicleId";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@VehicleId", id);
                    
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "Vehicle registration deleted successfully" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Vehicle registration not found" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while deleting the vehicle registration" });
            }
        }
    }
} 