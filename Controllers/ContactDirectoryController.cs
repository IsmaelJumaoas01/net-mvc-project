using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System.Collections.Generic;

namespace homeowner.Controllers
{
    public class ContactDirectoryController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";

        public IActionResult Index()
        {
            List<ContactDirectoryModel> contacts = new List<ContactDirectoryModel>();
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM contact_directory ORDER BY Department, Name";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                var reader = cmd.ExecuteReader();
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
                        OfficeHours = reader.GetString("OfficeHours")
                    });
                }
            }
            return View(contacts);
        }

        [HttpPost]
        public IActionResult AddContact(ContactDirectoryModel contact)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"INSERT INTO contact_directory 
                             (Name, Position, Department, PhoneNumber, Email, OfficeLocation, OfficeHours) 
                             VALUES (@Name, @Position, @Department, @PhoneNumber, @Email, @OfficeLocation, @OfficeHours)";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Name", contact.Name);
                cmd.Parameters.AddWithValue("@Position", contact.Position);
                cmd.Parameters.AddWithValue("@Department", contact.Department);
                cmd.Parameters.AddWithValue("@PhoneNumber", contact.PhoneNumber);
                cmd.Parameters.AddWithValue("@Email", contact.Email);
                cmd.Parameters.AddWithValue("@OfficeLocation", contact.OfficeLocation);
                cmd.Parameters.AddWithValue("@OfficeHours", contact.OfficeHours);
                cmd.ExecuteNonQuery();
            }

            return Json(new { success = true, message = "Contact added successfully." });
        }

        [HttpPost]
        public IActionResult UpdateContact(ContactDirectoryModel contact)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"UPDATE contact_directory 
                             SET Name = @Name, Position = @Position, Department = @Department, 
                                 PhoneNumber = @PhoneNumber, Email = @Email, 
                                 OfficeLocation = @OfficeLocation, OfficeHours = @OfficeHours 
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
                cmd.ExecuteNonQuery();
            }

            return Json(new { success = true, message = "Contact updated successfully." });
        }

        [HttpPost]
        public IActionResult DeleteContact(int contactId)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM contact_directory WHERE ContactID = @ContactID";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ContactID", contactId);
                cmd.ExecuteNonQuery();
            }

            return Json(new { success = true, message = "Contact deleted successfully." });
        }
    }
} 