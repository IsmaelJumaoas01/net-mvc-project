using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System.Collections.Generic;
using System;

namespace homeowner.Controllers
{
    public class FacilityReservationController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";

        public IActionResult Index()
        {
            string role = HttpContext.Session.GetString("Role") ?? "Staff";
            ViewBag.Role = role;

            if (role == "Administrator")
            {
                List<FacilityModel> facilities = new List<FacilityModel>();
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sqlFacilities = "SELECT * FROM facilities ORDER BY FacilityID ASC";
                    MySqlCommand cmd = new MySqlCommand(sqlFacilities, conn);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        facilities.Add(new FacilityModel
                        {
                            FacilityID = reader.GetInt32("FacilityID"),
                            FacilityName = reader.GetString("FacilityName")
                        });
                    }
                    reader.Close();
                }
                ViewBag.FacilityNames = facilities;
            }

            List<FacilityReservationModel> reservations = new List<FacilityReservationModel>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT r.*, f.FacilityName 
                       FROM facility_reservations r 
                       LEFT JOIN facilities f ON r.FacilityID = f.FacilityID 
                       ORDER BY r.ReservationDate DESC, r.StartTime ASC";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    reservations.Add(new FacilityReservationModel
                    {
                        ReservationID = reader.GetInt32("ReservationID"),
                        UserID = reader.IsDBNull(reader.GetOrdinal("UserID")) ? (int?)null : reader.GetInt32("UserID"),
                        FacilityID = reader.GetInt32("FacilityID"),
                        ReservationDate = reader.GetDateTime("ReservationDate"),
                        StartTime = reader.GetTimeSpan("StartTime"),
                        EndTime = reader.GetTimeSpan("EndTime"),
                        Status = reader.GetString("Status"),
                        FacilityName = reader.IsDBNull(reader.GetOrdinal("FacilityName")) ? "" : reader.GetString("FacilityName")
                    });
                }
                reader.Close();
            }

            return View("~/Views/Admin_Staff/Facility.cshtml", reservations);
        }

        [HttpPost]
        public IActionResult AddFacility(string facilityName)
        {
            string role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "Administrator")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }
            if (!string.IsNullOrWhiteSpace(facilityName))
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO facilities (FacilityName) VALUES (@FacilityName)";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@FacilityName", facilityName);
                    cmd.ExecuteNonQuery();
                }
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteFacility(int facilityID)
        {
            string role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "Administrator")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM facilities WHERE FacilityID = @FacilityID";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@FacilityID", facilityID);
                cmd.ExecuteNonQuery();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateReservationStatus(int reservationID, string status)
        {
            string role = HttpContext.Session.GetString("Role") ?? "";
            if (role != "Staff" && role != "Administrator")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "UPDATE facility_reservations SET Status = @Status WHERE ReservationID = @ReservationID";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@ReservationID", reservationID);
                cmd.ExecuteNonQuery();
            }
            return Json(new { success = true });
        }


        // HOMEOWNER: GET booking page
        public IActionResult Book()
        {
            // Ensure only Homeowners can access this page.
            string role = HttpContext.Session.GetString("Role") ?? "Homeowner";
            if (role != "Homeowner")
            {
                return RedirectToAction("Index", "Home");
            }

            // Load available facilities.
            List<FacilityModel> facilities = new List<FacilityModel>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sqlFacilities = "SELECT * FROM facilities ORDER BY FacilityID ASC";
                MySqlCommand cmd = new MySqlCommand(sqlFacilities, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    facilities.Add(new FacilityModel
                    {
                        FacilityID = reader.GetInt32("FacilityID"),
                        FacilityName = reader.GetString("FacilityName")
                    });
                }
                reader.Close();
            }
            ViewBag.Facilities = facilities;

            // Load current user's reservations.
            string userIdStr = HttpContext.Session.GetString("UserID");
            List<FacilityReservationModel> userReservations = new List<FacilityReservationModel>();
            if (!string.IsNullOrEmpty(userIdStr))
            {
                int userID = int.Parse(userIdStr);
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sqlReservations = @"SELECT r.*, f.FacilityName 
                                       FROM facility_reservations r 
                                       LEFT JOIN facilities f ON r.FacilityID = f.FacilityID 
                                       WHERE r.UserID = @UserID 
                                       ORDER BY r.ReservationDate DESC, r.StartTime ASC";
                    MySqlCommand cmd2 = new MySqlCommand(sqlReservations, conn);
                    cmd2.Parameters.AddWithValue("@UserID", userID);
                    var reader2 = cmd2.ExecuteReader();
                    while (reader2.Read())
                    {
                        userReservations.Add(new FacilityReservationModel
                        {
                            ReservationID = reader2.GetInt32("ReservationID"),
                            FacilityID = reader2.GetInt32("FacilityID"),
                            FacilityName = reader2.IsDBNull(reader2.GetOrdinal("FacilityName")) ? "" : reader2.GetString("FacilityName"),
                            ReservationDate = reader2.GetDateTime("ReservationDate"),
                            StartTime = reader2.GetTimeSpan("StartTime"),
                            EndTime = reader2.GetTimeSpan("EndTime"),
                            Status = reader2.GetString("Status")
                        });
                    }
                    reader2.Close();
                }
            }
            ViewBag.UserReservations = userReservations;

            return View("~/Views/Homeowner/BookFacility.cshtml");
        }


        [HttpPost]
        public IActionResult Book(int facilityID, DateTime reservationDate, TimeSpan startTime, TimeSpan endTime)
        {
            // Ensure only Homeowners can book
            string role = HttpContext.Session.GetString("Role") ?? "Homeowner";
            if (role != "Homeowner")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            // Get the logged-in user's ID from session.
            string userIdStr = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Json(new { success = false, message = "User not logged in" });
            }
            int userId = int.Parse(userIdStr);

            DateTime currentDate = DateTime.Now.Date; // Get today's date without the time
            TimeSpan currentTime = DateTime.Now.TimeOfDay; // Get the current time of the day

            // Prevent booking past times on the current day
            if (reservationDate.Date == currentDate && startTime < currentTime)
            {
                return Json(new { success = false, message = "Cannot book a past time slot on the current day." });
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Check if the user has already booked the same facility within the given time range.
                string checkOverlapQuery = @"
            SELECT COUNT(*) FROM facility_reservations 
            WHERE UserID = @UserID 
            AND FacilityID = @FacilityID 
            AND ReservationDate = @ReservationDate 
            AND (
                (@StartTime BETWEEN StartTime AND EndTime) 
                OR (@EndTime BETWEEN StartTime AND EndTime) 
                OR (StartTime BETWEEN @StartTime AND @EndTime)
                OR (EndTime BETWEEN @StartTime AND @EndTime)
            )";

                MySqlCommand checkCmd = new MySqlCommand(checkOverlapQuery, conn);
                checkCmd.Parameters.AddWithValue("@UserID", userId);
                checkCmd.Parameters.AddWithValue("@FacilityID", facilityID);
                checkCmd.Parameters.AddWithValue("@ReservationDate", reservationDate);
                checkCmd.Parameters.AddWithValue("@StartTime", startTime);
                checkCmd.Parameters.AddWithValue("@EndTime", endTime);

                int existingReservations = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (existingReservations > 0)
                {
                    return Json(new { success = false, message = "You have already booked this facility during the selected time slot." });
                }

                // If no conflicts, proceed with booking.
                string sql = "INSERT INTO facility_reservations (UserID, FacilityID, ReservationDate, StartTime, EndTime, Status) " +
                             "VALUES (@UserID, @FacilityID, @ReservationDate, @StartTime, @EndTime, 'Pending')";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@FacilityID", facilityID);
                cmd.Parameters.AddWithValue("@ReservationDate", reservationDate);
                cmd.Parameters.AddWithValue("@StartTime", startTime);
                cmd.Parameters.AddWithValue("@EndTime", endTime);
                cmd.ExecuteNonQuery();
            }

            return Json(new { success = true, message = "Reservation booked successfully!" });
        }


    }
}
