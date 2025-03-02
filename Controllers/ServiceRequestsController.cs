using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace homeowner.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";
        private readonly ILogger<ServiceRequestsController> _logger;

        public ServiceRequestsController(ILogger<ServiceRequestsController> logger)
        {
            _logger = logger;
        }

        // Get service requests for Homeowner or Staff
        public IActionResult Index()
        {
            string role = HttpContext.Session.GetString("Role") ?? "Staff";
            ViewBag.Role = role;

            List<ServiceRequestModel> serviceRequests = new List<ServiceRequestModel>();
            List<ServiceTypeModel> serviceTypes = new List<ServiceTypeModel>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = role == "Homeowner"
                    ? @"SELECT sr.*, st.ServiceTypeName  
                       FROM service_requests sr  
                       JOIN service_types st ON sr.ServiceTypeID = st.ServiceTypeID  
                       WHERE sr.UserID = @UserID 
                       ORDER BY sr.RequestDate DESC"
                    : @"SELECT sr.*, st.ServiceTypeName  
                       FROM service_requests sr  
                       JOIN service_types st ON sr.ServiceTypeID = st.ServiceTypeID  
                       ORDER BY sr.RequestDate DESC";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                if (role == "Homeowner")
                {
                    string userIdStr = HttpContext.Session.GetString("UserID");
                    if (!string.IsNullOrEmpty(userIdStr))
                    {
                        int userId = int.Parse(userIdStr);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                    }
                }

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    serviceRequests.Add(new ServiceRequestModel
                    {
                        RequestID = reader.GetInt32("RequestID"),
                        UserID = reader.GetInt32("UserID"),
                        ServiceTypeID = reader.GetInt32("ServiceTypeID"),
                        ServiceTypeName = reader.GetString("ServiceTypeName"),
                        Description = reader.GetString("Description"),
                        Status = reader.GetString("Status"),
                        RequestDate = reader.GetDateTime("RequestDate"),
                        ScheduledDate = reader.IsDBNull(reader.GetOrdinal("ServiceSchedule")) ? (DateTime?)null : reader.GetDateTime("ServiceSchedule")
                    });
                }
                reader.Close();
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM service_types ORDER BY ServiceTypeID ASC";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    serviceTypes.Add(new ServiceTypeModel
                    {
                        ServiceTypeID = reader.GetInt32("ServiceTypeID"),
                        ServiceTypeName = reader.GetString("ServiceTypeName")
                    });
                }
                reader.Close();
            }

            ViewBag.ServiceRequests = serviceRequests;
            ViewBag.ServiceTypes = serviceTypes;

            // Initialize ViewBag.UserReservations to avoid null reference
            ViewBag.UserReservations = new List<FacilityReservationModel>();

            return View("~/Views/Homeowner/ServiceRequest.cshtml");
        }

        public IActionResult RequestService()
        {
            string role = HttpContext.Session.GetString("Role") ?? "Homeowner";
            if (role != "Homeowner")
            {
                return RedirectToAction("Index", "Home");
            }

            List<ServiceTypeModel> serviceTypes = new List<ServiceTypeModel>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM service_types ORDER BY ServiceTypeID ASC";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    serviceTypes.Add(new ServiceTypeModel
                    {
                        ServiceTypeID = reader.GetInt32("ServiceTypeID"),
                        ServiceTypeName = reader.GetString("ServiceTypeName")
                    });
                }
                reader.Close();
            }

            // Ensure ViewBag.ServiceTypes is never null
            ViewBag.ServiceTypes = serviceTypes.Count > 0 ? serviceTypes : new List<ServiceTypeModel>();

            // Initialize ViewBag.UserReservations to avoid null reference
            ViewBag.UserReservations = new List<FacilityReservationModel>();

            // Return the correct view
            return View("~/Views/Homeowner/ServiceRequest.cshtml");
        }

        [HttpPost]
        public IActionResult RequestService(int serviceTypeID, string description, DateTime scheduledDateTime)
        {
            _logger.LogInformation("RequestService called with parameters: serviceTypeID={serviceTypeID}, description={description}, scheduledDateTime={scheduledDateTime}", serviceTypeID, description, scheduledDateTime);

            string role = HttpContext.Session.GetString("Role") ?? "Homeowner";
            if (role != "Homeowner")
            {
                _logger.LogWarning("Unauthorized access attempt by role: {role}", role);
                return Json(new { success = false, message = "Unauthorized" });
            }

            string userIdStr = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userIdStr))
            {
                _logger.LogWarning("User not logged in");
                return Json(new { success = false, message = "User not logged in" });
            }
            int userId = int.Parse(userIdStr);

            DateTime currentDate = DateTime.Now;
            if (scheduledDateTime < currentDate)
            {
                _logger.LogWarning("Scheduled date and time must be in the future. scheduledDateTime={scheduledDateTime}, currentDate={currentDate}", scheduledDateTime, currentDate);
                return Json(new { success = false, message = "Scheduled date and time must be in the future." });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO service_requests (UserID, ServiceTypeID, Description, Status, RequestDate, ServiceSchedule) " +
                                 "VALUES (@UserID, @ServiceTypeID, @Description, 'Pending', NOW(), @ServiceSchedule)";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@ServiceTypeID", serviceTypeID);
                    cmd.Parameters.AddWithValue("@Description", description);
                    cmd.Parameters.AddWithValue("@ServiceSchedule", scheduledDateTime);
                    cmd.ExecuteNonQuery();
                }

                _logger.LogInformation("Service request submitted successfully for userId={userId}", userId);
                return Json(new { success = true, message = "Service request submitted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while submitting service request for userId={userId}", userId);
                return Json(new { success = false, message = "An error occurred while submitting the service request." });
            }
        }
    }
}