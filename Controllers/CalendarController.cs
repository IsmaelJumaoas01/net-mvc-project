using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using homeowner.Models;

namespace homeowner.Controllers
{
    public class CalendarController : Controller
    {
        private readonly string _connectionString;

        public CalendarController(IConfiguration configuration)
        {
            _connectionString = "Server=localhost;Database=homeowners_db;Uid=root;Pwd=;";
        }

        public IActionResult Index()
        {
            var events = new List<CalendarEvent>();
            var userIdStr = HttpContext.Session.GetString("UserID");
            var userId = !string.IsNullOrEmpty(userIdStr) ? int.Parse(userIdStr) : 0;
            var role = HttpContext.Session.GetString("Role");

            Console.WriteLine($"User ID: {userId}, Role: {role}");

            if (userId == 0)
            {
                return RedirectToAction("Login", "LoginRegister");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                // Get Facility Reservations
                var facilityQuery = role == "Administrator" || role == "Staff" ?
                    @"SELECT fr.ReservationID, f.FacilityName, fr.ReservationDate, fr.StartTime, fr.EndTime, u.Username 
                    FROM facility_reservations fr 
                    JOIN facilities f ON fr.FacilityID = f.FacilityID 
                    JOIN users u ON fr.UserID = u.UserID 
                    WHERE fr.Status = 'Confirmed'" :
                    @"SELECT fr.ReservationID, f.FacilityName, fr.ReservationDate, fr.StartTime, fr.EndTime, u.Username 
                    FROM facility_reservations fr 
                    JOIN facilities f ON fr.FacilityID = f.FacilityID 
                    JOIN users u ON fr.UserID = u.UserID 
                    WHERE fr.UserID = @UserId AND fr.Status = 'Confirmed'";

                Console.WriteLine("Executing facility query: " + facilityQuery);
                using (var command = new MySqlCommand(facilityQuery, connection))
                {
                    if (role == "Homeowner")
                        command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var date = reader.GetDateTime("ReservationDate");
                            var startTime = reader.GetTimeSpan("StartTime");
                            var endTime = reader.GetTimeSpan("EndTime");
                            var start = date.Date + startTime;
                            var end = date.Date + endTime;

                            events.Add(new CalendarEvent
                            {
                                Id = "FR" + reader.GetInt32("ReservationID"),
                                Title = $"Facility Booking: {reader.GetString("FacilityName")}",
                                Start = start,
                                End = end,
                                Description = $"Booked by {reader.GetString("Username")}",
                                Color = "#4CAF50", // Green
                                AllDay = false
                            });
                            Console.WriteLine($"Added facility event: {reader.GetString("FacilityName")} on {start}");
                        }
                    }
                }

                // Get Service Requests
                var serviceQuery = role == "Administrator" || role == "Staff" ?
                    @"SELECT sr.RequestID, sr.Description, sr.RequestDate, sr.ServiceSchedule, sr.Status, u.Username, st.ServiceTypeName 
                    FROM service_requests sr 
                    JOIN users u ON sr.UserID = u.UserID 
                    JOIN service_types st ON sr.ServiceTypeID = st.ServiceTypeID 
                    WHERE sr.Status IN ('Pending', 'In Progress')" :
                    @"SELECT sr.RequestID, sr.Description, sr.RequestDate, sr.ServiceSchedule, sr.Status, st.ServiceTypeName 
                    FROM service_requests sr 
                    JOIN service_types st ON sr.ServiceTypeID = st.ServiceTypeID 
                    WHERE sr.UserID = @UserId AND sr.Status IN ('Pending', 'In Progress')";

                Console.WriteLine("Executing service query: " + serviceQuery);
                using (var command = new MySqlCommand(serviceQuery, connection))
                {
                    if (role == "Homeowner")
                        command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var requestDate = reader.GetDateTime("RequestDate");
                            var serviceSchedule = reader.IsDBNull(reader.GetOrdinal("ServiceSchedule")) ? 
                                requestDate : reader.GetDateTime("ServiceSchedule");
                            
                            var title = role == "Administrator" || role == "Staff" ?
                                $"Service Request: {reader.GetString("Description")} ({reader.GetString("Username")})" :
                                $"Service Request: {reader.GetString("Description")}";

                            events.Add(new CalendarEvent
                            {
                                Id = "SR" + reader.GetInt32("RequestID"),
                                Title = title,
                                Start = serviceSchedule,
                                End = serviceSchedule.AddHours(1),
                                Description = $"Service Type: {reader.GetString("ServiceTypeName")}\nStatus: {reader.GetString("Status")}",
                                Color = "#E91E63" // Pink
                            });
                            Console.WriteLine($"Added service event: {reader.GetString("Description")} on {serviceSchedule}");
                        }
                    }
                }

                // Get Visitor Passes
                var visitorQuery = role == "Administrator" || role == "Staff" ?
                    @"SELECT vp.VisitorPassId, vp.VisitorName, vp.VisitDate, vp.VisitTime, u.Username 
                    FROM visitor_passes vp 
                    JOIN users u ON vp.UserID = u.UserID 
                    WHERE vp.Status = 'Approved'" :
                    @"SELECT VisitorPassId, VisitorName, VisitDate, VisitTime 
                    FROM visitor_passes 
                    WHERE UserID = @UserId AND Status = 'Approved'";

                Console.WriteLine("Executing visitor query: " + visitorQuery);
                using (var command = new MySqlCommand(visitorQuery, connection))
                {
                    if (role == "Homeowner")
                        command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var visitDate = reader.GetDateTime("VisitDate");
                            var visitTime = reader.GetTimeSpan("VisitTime");
                            var start = visitDate.Date + visitTime;
                            var end = start.AddHours(2); // 2-hour duration

                            var title = role == "Administrator" || role == "Staff" ?
                                $"Visitor Pass: {reader.GetString("VisitorName")} ({reader.GetString("Username")})" :
                                $"Visitor Pass: {reader.GetString("VisitorName")}";

                            events.Add(new CalendarEvent
                            {
                                Id = "VP" + reader.GetInt32("VisitorPassId"),
                                Title = title,
                                Start = start,
                                End = end,
                                Description = "Visitor Pass",
                                Color = "#2196F3", // Blue
                                AllDay = false
                            });
                            Console.WriteLine($"Added visitor event: {reader.GetString("VisitorName")} on {start}");
                        }
                    }
                }

                // Get Announcements
                var announcementQuery = @"SELECT a.AnnouncementID, a.Title, a.Content, a.CreatedAt, u.Username 
                                      FROM announcements a 
                                      JOIN users u ON a.UserID = u.UserID 
                                      WHERE a.CreatedAt >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)
                                      ORDER BY a.CreatedAt DESC";

                Console.WriteLine("Executing announcement query: " + announcementQuery);
                using (var command = new MySqlCommand(announcementQuery, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var createdAt = reader.GetDateTime("CreatedAt");
                            // Show announcements for 3 days from creation date
                            var endDate = createdAt.AddDays(3);
                            
                            // If the announcement is older than 3 days, skip it
                            if (endDate < DateTime.Now)
                            {
                                continue;
                            }

                            var title = reader.GetString("Title").Replace("[b]", "").Replace("[/b]", ""); // Remove BBCode
                            var content = reader.GetString("Content");
                            var username = reader.GetString("Username");

                            events.Add(new CalendarEvent
                            {
                                Id = "AN" + reader.GetInt32("AnnouncementID"),
                                Title = $"📢 {title}",
                                Start = createdAt,
                                End = endDate,
                                Description = $"Posted by: {username}\n\n{content}",
                                Color = "#9C27B0", // Purple
                                AllDay = true
                            });
                            Console.WriteLine($"Added announcement event: {title} from {createdAt} to {endDate}");
                        }
                    }
                }

                // Get Bills Due Dates
                var billsQuery = role == "Administrator" || role == "Staff" ?
                    @"SELECT b.BillID, b.DueDate, b.Amount, bt.Name as BillTypeName, u.Username 
                    FROM bills b 
                    JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID 
                    JOIN users u ON b.UserID = u.UserID 
                    WHERE b.Status = 'Pending' OR b.Status = 'Overdue'" :
                    @"SELECT b.BillID, b.DueDate, b.Amount, bt.Name as BillTypeName 
                    FROM bills b 
                    JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID 
                    WHERE b.UserID = @UserId AND (b.Status = 'Pending' OR b.Status = 'Overdue')";

                Console.WriteLine("Executing bills query: " + billsQuery);
                using (var command = new MySqlCommand(billsQuery, connection))
                {
                    if (role == "Homeowner")
                        command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var dueDate = reader.GetDateTime("DueDate");
                            var amount = reader.GetDecimal("Amount");
                            var title = role == "Administrator" || role == "Staff" ?
                                $"Bill Due: {reader.GetString("BillTypeName")} - {reader.GetString("Username")}" :
                                $"Bill Due: {reader.GetString("BillTypeName")}";

                            events.Add(new CalendarEvent
                            {
                                Id = "BL" + reader.GetInt32("BillID"),
                                Title = title,
                                Start = dueDate,
                                End = dueDate.AddDays(1),
                                Description = $"Amount Due: ₱{amount:N2}",
                                Color = "#F44336" // Red
                            });
                            Console.WriteLine($"Added bill event: {title} due on {dueDate}");
                        }
                    }
                }
            }

            // Serialize events to JSON and pass to view
            var serializedEvents = System.Text.Json.JsonSerializer.Serialize(events, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = null, // Keep property names as is
                WriteIndented = true,
                Converters = 
                {
                    new System.Text.Json.Serialization.JsonStringEnumConverter()
                }
            });
            ViewBag.Events = serializedEvents;

            Console.WriteLine($"Total events found: {events.Count}");
            Console.WriteLine($"Serialized events: {serializedEvents}");

            return View();
        }

        [HttpGet]
        public IActionResult GetEvents(DateTime start, DateTime end)
        {
            // This method can be used for AJAX loading of events if needed
            return Json(new { });
        }
    }
} 