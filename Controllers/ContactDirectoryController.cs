using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System.Collections.Generic;
using System;

namespace homeowner.Controllers
{
    public class ContactDirectoryController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";

        // GET: ContactDirectory
        public IActionResult Index()
        {
            string role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Index", "Home");
            }

            List<ContactDirectoryModel> contacts = new List<ContactDirectoryModel>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM contact_directory ORDER BY Department, Name";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        contacts.Add(new ContactDirectoryModel
                        {
                            ContactID = reader.GetInt32("ContactID"),
                            Name = reader.GetString("Name"),
                            Position = reader.GetString("Position"),
                            Department = reader.GetString("Department"),
                            PhoneNumber = reader.GetString("PhoneNumber"),
                            Email = reader.GetString("Email"),
                            OfficeLocation = reader.GetString("OfficeLocation"),
                            OfficeHours = reader.GetString("OfficeHours"),
                            CreatedAt = reader.GetDateTime("CreatedAt"),
                            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : (DateTime?)reader.GetDateTime("UpdatedAt")
                        });
                    }
                }
            }

            ViewBag.IsAdmin = role == "Administrator";
            return View(contacts);
        }

        // GET: ContactDirectory/Details/5
        public IActionResult Details(int id)
        {
            ContactDirectoryModel contact = null;
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM contact_directory WHERE ContactID = @ContactID";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ContactID", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        contact = new ContactDirectoryModel
                        {
                            ContactID = reader.GetInt32("ContactID"),
                            Name = reader.GetString("Name"),
                            Position = reader.GetString("Position"),
                            Department = reader.GetString("Department"),
                            PhoneNumber = reader.GetString("PhoneNumber"),
                            Email = reader.GetString("Email"),
                            OfficeLocation = reader.GetString("OfficeLocation"),
                            OfficeHours = reader.GetString("OfficeHours"),
                            CreatedAt = reader.GetDateTime("CreatedAt"),
                            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : (DateTime?)reader.GetDateTime("UpdatedAt")
                        };
                    }
                }
            }

            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: ContactDirectory/Create
        [HttpPost]
        public IActionResult Create([FromBody] ContactDirectoryModel contact)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data" });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO contact_directory 
                                 (Name, Position, Department, PhoneNumber, Email, OfficeLocation, OfficeHours, CreatedAt) 
                                 VALUES (@Name, @Position, @Department, @PhoneNumber, @Email, @OfficeLocation, @OfficeHours, @CreatedAt)";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Name", contact.Name);
                    cmd.Parameters.AddWithValue("@Position", contact.Position);
                    cmd.Parameters.AddWithValue("@Department", contact.Department);
                    cmd.Parameters.AddWithValue("@PhoneNumber", contact.PhoneNumber);
                    cmd.Parameters.AddWithValue("@Email", contact.Email);
                    cmd.Parameters.AddWithValue("@OfficeLocation", contact.OfficeLocation);
                    cmd.Parameters.AddWithValue("@OfficeHours", contact.OfficeHours);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    cmd.ExecuteNonQuery();

                    // Get the newly created contact
                    long newContactId = cmd.LastInsertedId;
                    sql = "SELECT * FROM contact_directory WHERE ContactID = @ContactID";
                    cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@ContactID", newContactId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            contact = new ContactDirectoryModel
                            {
                                ContactID = reader.GetInt32("ContactID"),
                                Name = reader.GetString("Name"),
                                Position = reader.GetString("Position"),
                                Department = reader.GetString("Department"),
                                PhoneNumber = reader.GetString("PhoneNumber"),
                                Email = reader.GetString("Email"),
                                OfficeLocation = reader.GetString("OfficeLocation"),
                                OfficeHours = reader.GetString("OfficeHours"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : (DateTime?)reader.GetDateTime("UpdatedAt")
                            };
                        }
                    }
                }

                return Json(new { success = true, message = "Contact created successfully", contact = contact });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating contact: " + ex.Message });
            }
        }

        // POST: ContactDirectory/Edit
        [HttpPost]
        public IActionResult Edit([FromBody] ContactDirectoryModel contact)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data" });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"UPDATE contact_directory 
                                 SET Name = @Name, Position = @Position, Department = @Department, 
                                     PhoneNumber = @PhoneNumber, Email = @Email, 
                                     OfficeLocation = @OfficeLocation, OfficeHours = @OfficeHours,
                                     UpdatedAt = @UpdatedAt
                                 WHERE ContactID = @ContactID";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@ContactID", contact.ContactID);
                    cmd.Parameters.AddWithValue("@Name", contact.Name);
                    cmd.Parameters.AddWithValue("@Position", contact.Position);
                    cmd.Parameters.AddWithValue("@Department", contact.Department);
                    cmd.Parameters.AddWithValue("@PhoneNumber", contact.PhoneNumber);
                    cmd.Parameters.AddWithValue("@Email", contact.Email);
                    cmd.Parameters.AddWithValue("@OfficeLocation", contact.OfficeLocation);
                    cmd.Parameters.AddWithValue("@OfficeHours", contact.OfficeHours);
                    cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                    cmd.ExecuteNonQuery();

                    // Get the updated contact
                    sql = "SELECT * FROM contact_directory WHERE ContactID = @ContactID";
                    cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@ContactID", contact.ContactID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            contact = new ContactDirectoryModel
                            {
                                ContactID = reader.GetInt32("ContactID"),
                                Name = reader.GetString("Name"),
                                Position = reader.GetString("Position"),
                                Department = reader.GetString("Department"),
                                PhoneNumber = reader.GetString("PhoneNumber"),
                                Email = reader.GetString("Email"),
                                OfficeLocation = reader.GetString("OfficeLocation"),
                                OfficeHours = reader.GetString("OfficeHours"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : (DateTime?)reader.GetDateTime("UpdatedAt")
                            };
                        }
                    }
                }

                return Json(new { success = true, message = "Contact updated successfully", contact = contact });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating contact: " + ex.Message });
            }
        }

        // POST: ContactDirectory/Delete/5
        [HttpPost]
        public IActionResult Delete(int id)
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
                    string sql = "DELETE FROM contact_directory WHERE ContactID = @ContactID";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@ContactID", id);
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Contact deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting contact: " + ex.Message });
            }
        }

        // GET: ContactDirectory/Search
        [HttpGet]
        public IActionResult Search(string query, string department)
        {
            List<ContactDirectoryModel> contacts = new List<ContactDirectoryModel>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT * FROM contact_directory 
                             WHERE (@Query IS NULL OR Name LIKE @QueryLike 
                                   OR Position LIKE @QueryLike 
                                   OR Department LIKE @QueryLike)
                             AND (@Department IS NULL OR Department = @Department)
                             ORDER BY Department, Name";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Query", string.IsNullOrEmpty(query) ? null : query);
                cmd.Parameters.AddWithValue("@QueryLike", $"%{query}%");
                cmd.Parameters.AddWithValue("@Department", string.IsNullOrEmpty(department) ? null : department);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        contacts.Add(new ContactDirectoryModel
                        {
                            ContactID = reader.GetInt32("ContactID"),
                            Name = reader.GetString("Name"),
                            Position = reader.GetString("Position"),
                            Department = reader.GetString("Department"),
                            PhoneNumber = reader.GetString("PhoneNumber"),
                            Email = reader.GetString("Email"),
                            OfficeLocation = reader.GetString("OfficeLocation"),
                            OfficeHours = reader.GetString("OfficeHours"),
                            CreatedAt = reader.GetDateTime("CreatedAt"),
                            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : (DateTime?)reader.GetDateTime("UpdatedAt")
                        });
                    }
                }
            }

            return Json(contacts);
        }

        // GET: ContactDirectory/Manage
        public IActionResult Manage()
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return RedirectToAction("Index", "Home");
            }

            List<ContactDirectoryModel> contacts = new List<ContactDirectoryModel>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM contact_directory ORDER BY Department, Name";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        contacts.Add(new ContactDirectoryModel
                        {
                            ContactID = reader.GetInt32("ContactID"),
                            Name = reader.GetString("Name"),
                            Position = reader.GetString("Position"),
                            Department = reader.GetString("Department"),
                            PhoneNumber = reader.GetString("PhoneNumber"),
                            Email = reader.GetString("Email"),
                            OfficeLocation = reader.GetString("OfficeLocation"),
                            OfficeHours = reader.GetString("OfficeHours"),
                            CreatedAt = reader.GetDateTime("CreatedAt"),
                            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : (DateTime?)reader.GetDateTime("UpdatedAt")
                        });
                    }
                }
            }

            return View(contacts);
        }
    }
} 