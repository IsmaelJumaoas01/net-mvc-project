using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace homeowner.Controllers
{
    public class BillingController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";

        // GET: Billing/Dashboard (Homeowner View)
        public IActionResult Dashboard()
        {
            string role = HttpContext.Session.GetString("Role");
            int userId = Convert.ToInt32(HttpContext.Session.GetString("UserID"));

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Index", "Home");
            }

            var dashboard = GetDashboardData(userId);
            ViewBag.Role = role;
            return View(dashboard);
        }

        // GET: Billing/AdminDashboard
        public IActionResult AdminDashboard()
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return RedirectToAction("Index", "Home");
            }

            var stats = GetAdminDashboardStats();
            ViewBag.Role = role;
            return View(stats);
        }

        // GET: Billing/ManageBills
        public IActionResult ManageBills()
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return RedirectToAction("Index", "Home");
            }

            var bills = GetAllBills();
            ViewBag.BillTypes = GetBillTypes();
            ViewBag.Users = GetUsers();
            return View(bills);
        }

        // POST: Billing/GenerateBill
        [HttpPost]
        public IActionResult GenerateBill([FromBody] BulkBillGenerationViewModel model)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
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
                    foreach (var userId in model.SelectedUsers)
                    {
                        string sql = @"INSERT INTO bills 
                                     (UserID, BillTypeID, Amount, DueDate, Status, Description, CreatedAt, UpdatedAt) 
                                     VALUES (@UserID, @BillTypeID, @Amount, @DueDate, 'Pending', @Description, @CreatedAt, @UpdatedAt)";
                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@BillTypeID", model.BillTypeID);
                        cmd.Parameters.AddWithValue("@Amount", model.Amount);
                        cmd.Parameters.AddWithValue("@DueDate", model.DueDate);
                        cmd.Parameters.AddWithValue("@Description", model.Description ?? "");
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Bills generated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error generating bills: " + ex.Message });
            }
        }

        // GET: Billing/GetPendingPayments
        public IActionResult GetPendingPayments()
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                var payments = new List<Payment>();
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT p.PaymentID, p.Amount, p.PaymentDate, p.PaymentMethod, 
                                        p.ReferenceNumber, p.Status, p.Notes, p.CreatedAt,
                                        b.BillID, b.Amount as BillAmount, bt.Name as BillTypeName,
                                        u.Username, u.FirstName, u.LastName,
                                        CASE WHEN p.ProofOfPayment IS NOT NULL THEN 1 ELSE 0 END as HasProofOfPayment
                                 FROM payments p 
                                 JOIN bills b ON p.BillID = b.BillID 
                                 JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID 
                                 JOIN users u ON p.UserID = u.UserID 
                                 WHERE p.Status = 'Pending' 
                                 ORDER BY p.CreatedAt DESC";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            payments.Add(new Payment
                            {
                                PaymentID = reader.GetInt32("PaymentID"),
                                Amount = reader.GetDecimal("Amount"),
                                PaymentDate = reader.GetDateTime("PaymentDate"),
                                PaymentMethod = reader.GetString("PaymentMethod"),
                                ReferenceNumber = reader.GetString("ReferenceNumber"),
                                Status = reader.GetString("Status"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString("Notes"),
                                HasProofOfPayment = reader.GetBoolean("HasProofOfPayment"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                Bill = new Bill
                                {
                                    BillID = reader.GetInt32("BillID"),
                                    Amount = reader.GetDecimal("BillAmount"),
                                    BillType = new BillType { Name = reader.GetString("BillTypeName") }
                                },
                                User = new UserModel
                                {
                                    Username = reader.GetString("Username"),
                                    FirstName = reader.GetString("FirstName"),
                                    LastName = reader.GetString("LastName")
                                }
                            });
                        }
                    }
                }

                return Json(new { success = true, payments = payments });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving pending payments: " + ex.Message });
            }
        }

        // GET: Billing/GetProofOfPayment/{paymentId}
        public IActionResult GetProofOfPayment(int paymentId)
        {
            string role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role))
            {
                return Unauthorized();
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT ProofOfPayment FROM payments WHERE PaymentID = @PaymentID";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@PaymentID", paymentId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && !reader.IsDBNull(reader.GetOrdinal("ProofOfPayment")))
                        {
                            byte[] fileData = (byte[])reader["ProofOfPayment"];
                            return File(fileData, "image/jpeg");
                        }
                    }
                }

                return NotFound();
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }

        // POST: Billing/SubmitPayment
        [HttpPost]
        public async Task<IActionResult> SubmitPayment([FromForm] BillPaymentViewModel model)
        {
            string role = HttpContext.Session.GetString("Role");
            int userId = Convert.ToInt32(HttpContext.Session.GetString("UserID"));

            if (string.IsNullOrEmpty(role))
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                byte[] proofOfPaymentData = null;

                if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.ProofOfPayment.CopyToAsync(memoryStream);
                        proofOfPaymentData = memoryStream.ToArray();
                    }
                }

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Insert payment record
                            string sql = @"INSERT INTO payments 
                                     (BillID, UserID, Amount, PaymentDate, PaymentMethod, ReferenceNumber, 
                                      ProofOfPayment, Status, Notes, CreatedAt) 
                                     VALUES (@BillID, @UserID, @Amount, @PaymentDate, @PaymentMethod, 
                                            @ReferenceNumber, @ProofOfPayment, 'Pending', @Notes, @CreatedAt)";
                            MySqlCommand cmd = new MySqlCommand(sql, conn);
                            cmd.Parameters.AddWithValue("@BillID", model.BillID);
                            cmd.Parameters.AddWithValue("@UserID", userId);
                            cmd.Parameters.AddWithValue("@Amount", model.Amount);
                            cmd.Parameters.AddWithValue("@PaymentDate", model.PaymentDate);
                            cmd.Parameters.AddWithValue("@PaymentMethod", model.PaymentMethod);
                            cmd.Parameters.AddWithValue("@ReferenceNumber", model.ReferenceNumber);
                            cmd.Parameters.AddWithValue("@ProofOfPayment", proofOfPaymentData ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            cmd.ExecuteNonQuery();

                            // Update bill status to Pending
                            sql = @"UPDATE bills SET Status = 'Pending', UpdatedAt = @UpdatedAt WHERE BillID = @BillID";
                            cmd = new MySqlCommand(sql, conn);
                            cmd.Parameters.AddWithValue("@BillID", model.BillID);
                            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                            cmd.ExecuteNonQuery();

                            transaction.Commit();
                            return Json(new { success = true, message = "Payment submitted successfully" });
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error submitting payment: " + ex.Message });
            }
        }

        // POST: Billing/VerifyPayment
        [HttpPost]
        public IActionResult VerifyPayment(int paymentId, bool isApproved, string notes)
        {
            string role = HttpContext.Session.GetString("Role");
            int userId = Convert.ToInt32(HttpContext.Session.GetString("UserID"));

            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string newStatus = isApproved ? "Verified" : "Rejected";
                            int billId = 0;
                            decimal paymentAmount = 0;
                            
                            // Get bill ID and payment amount
                            string sql = "SELECT BillID, Amount FROM payments WHERE PaymentID = @PaymentID";
                            MySqlCommand cmd = new MySqlCommand(sql, conn);
                            cmd.Parameters.AddWithValue("@PaymentID", paymentId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    billId = reader.GetInt32("BillID");
                                    paymentAmount = reader.GetDecimal("Amount");
                                }
                            }
                            
                            // Update payment status
                            sql = @"UPDATE payments 
                                   SET Status = @Status, 
                                       Notes = @Notes, 
                                       VerifiedAt = @VerifiedAt, 
                                       VerifiedBy = @VerifiedBy 
                                   WHERE PaymentID = @PaymentID";
                            cmd = new MySqlCommand(sql, conn);
                            cmd.Parameters.AddWithValue("@Status", newStatus);
                            cmd.Parameters.AddWithValue("@Notes", notes);
                            cmd.Parameters.AddWithValue("@VerifiedAt", DateTime.Now);
                            cmd.Parameters.AddWithValue("@VerifiedBy", userId);
                            cmd.Parameters.AddWithValue("@PaymentID", paymentId);
                            cmd.ExecuteNonQuery();

                            // Update bill status
                            sql = @"UPDATE bills 
                                   SET Status = @Status, 
                                       UpdatedAt = @UpdatedAt 
                                   WHERE BillID = @BillID";
                            cmd = new MySqlCommand(sql, conn);
                            cmd.Parameters.AddWithValue("@BillID", billId);
                            cmd.Parameters.AddWithValue("@Status", isApproved ? "Paid" : "Pending");
                            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                            cmd.ExecuteNonQuery();

                            // Add to payment history
                            sql = @"INSERT INTO payment_history 
                                   (PaymentID, Action, NewStatus, Notes, PerformedBy, CreatedAt) 
                                   VALUES (@PaymentID, @Action, @NewStatus, @Notes, @PerformedBy, @CreatedAt)";
                            cmd = new MySqlCommand(sql, conn);
                            cmd.Parameters.AddWithValue("@PaymentID", paymentId);
                            cmd.Parameters.AddWithValue("@Action", isApproved ? "Verify" : "Reject");
                            cmd.Parameters.AddWithValue("@NewStatus", newStatus);
                            cmd.Parameters.AddWithValue("@Notes", notes);
                            cmd.Parameters.AddWithValue("@PerformedBy", userId);
                            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            cmd.ExecuteNonQuery();

                            transaction.Commit();

                            // Get updated bill and payment data
                            var updatedData = GetBillAndPaymentData(billId);
                            return Json(new { 
                                success = true, 
                                message = $"Payment {(isApproved ? "verified" : "rejected")} successfully",
                                updatedData = updatedData
                            });
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error processing payment: {ex.Message}" });
            }
        }

        private object GetBillAndPaymentData(int billId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT b.*, bt.Name as BillTypeName,
                                    p.PaymentID, p.Status as PaymentStatus, p.PaymentDate,
                                    p.PaymentMethod, p.ReferenceNumber
                             FROM bills b
                             JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID
                             LEFT JOIN payments p ON b.BillID = p.BillID
                             WHERE b.BillID = @BillID
                             ORDER BY p.CreatedAt DESC LIMIT 1";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@BillID", billId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            billId = reader.GetInt32("BillID"),
                            status = reader.GetString("Status"),
                            amount = reader.GetDecimal("Amount"),
                            dueDate = reader.GetDateTime("DueDate"),
                            billType = reader.GetString("BillTypeName"),
                            paymentId = reader.IsDBNull(reader.GetOrdinal("PaymentID")) ? (int?)null : reader.GetInt32("PaymentID"),
                            paymentStatus = reader.IsDBNull(reader.GetOrdinal("PaymentStatus")) ? null : reader.GetString("PaymentStatus"),
                            paymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ? (DateTime?)null : reader.GetDateTime("PaymentDate"),
                            paymentMethod = reader.IsDBNull(reader.GetOrdinal("PaymentMethod")) ? null : reader.GetString("PaymentMethod"),
                            referenceNumber = reader.IsDBNull(reader.GetOrdinal("ReferenceNumber")) ? null : reader.GetString("ReferenceNumber")
                        };
                    }
                }
            }
            return null;
        }

        // Model class for bill status update
        public class BillStatusUpdateModel
        {
            public int billId { get; set; }
            public string status { get; set; }
            public string notes { get; set; }
        }

        // POST: Billing/UpdateBillStatus
        [HttpPost]
        public IActionResult UpdateBillStatus([FromBody] BillStatusUpdateModel model)
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
                    string sql = @"UPDATE bills 
                                 SET Status = @Status, UpdatedAt = @UpdatedAt 
                                 WHERE BillID = @BillID";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Status", model.status);
                    cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@BillID", model.billId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "Bill status updated successfully" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Bill not found or no changes made" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating bill status: " + ex.Message });
            }
        }

        // POST: Billing/ManageOverdueBills
        [HttpPost]
        public IActionResult ManageOverdueBills()
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
                    string sql = @"UPDATE bills 
                                 SET Status = 'Overdue', UpdatedAt = @UpdatedAt 
                                 WHERE Status = 'Pending' 
                                 AND DueDate < CURDATE()";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                    int affected = cmd.ExecuteNonQuery();

                    return Json(new { success = true, message = $"{affected} bills marked as overdue" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error managing overdue bills: " + ex.Message });
            }
        }

        // POST: Billing/CreateBillType
        [HttpPost]
        public IActionResult CreateBillType([FromBody] BillType model)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
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
                    string sql = @"INSERT INTO bill_types 
                                 (Name, Description, IsRecurring, CreatedAt) 
                                 VALUES (@Name, @Description, @IsRecurring, @CreatedAt)";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Name", model.Name);
                    cmd.Parameters.AddWithValue("@Description", model.Description ?? "");
                    cmd.Parameters.AddWithValue("@IsRecurring", model.IsRecurring);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    cmd.ExecuteNonQuery();

                    return Json(new { success = true, message = "Bill type created successfully" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating bill type: " + ex.Message });
            }
        }

        // POST: Billing/GenerateMonthlyBills
        [HttpPost]
        public IActionResult GenerateMonthlyBills([FromBody] BulkBillGenerationViewModel model)
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
                    // Get all homeowners
                    string sql = "SELECT UserID FROM users WHERE Role = 'Homeowner'";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    List<int> homeownerIds = new List<int>();
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            homeownerIds.Add(reader.GetInt32("UserID"));
                        }
                    }

                    // Generate bills for each homeowner
                    foreach (var userId in homeownerIds)
                    {
                        sql = @"INSERT INTO bills 
                               (UserID, BillTypeID, Amount, DueDate, Status, Description, CreatedAt, UpdatedAt) 
                               VALUES (@UserID, @BillTypeID, @Amount, @DueDate, 'Pending', @Description, @CreatedAt, @UpdatedAt)";
                        cmd = new MySqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@BillTypeID", model.BillTypeID);
                        cmd.Parameters.AddWithValue("@Amount", model.Amount);
                        cmd.Parameters.AddWithValue("@DueDate", model.DueDate);
                        cmd.Parameters.AddWithValue("@Description", model.Description ?? "Monthly bill for " + DateTime.Now.ToString("MMMM yyyy"));
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Monthly bills generated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error generating monthly bills: " + ex.Message });
            }
        }

        // GET: Billing/GetBillDetails/{id}
        public IActionResult GetBillDetails(int id)
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
                    string sql = @"SELECT b.*, bt.Name as BillTypeName, u.Username 
                                 FROM bills b 
                                 JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID 
                                 JOIN users u ON b.UserID = u.UserID 
                                 WHERE b.BillID = @BillID";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@BillID", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var bill = new Bill
                            {
                                BillID = reader.GetInt32("BillID"),
                                Amount = reader.GetDecimal("Amount"),
                                DueDate = reader.GetDateTime("DueDate"),
                                Status = reader.GetString("Status"),
                                Description = reader.GetString("Description"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : (DateTime?)reader.GetDateTime("UpdatedAt"),
                                BillType = new BillType { Name = reader.GetString("BillTypeName") },
                                User = new UserModel { Username = reader.GetString("Username") }
                            };

                            return Json(new { success = true, bill = bill });
                        }
                    }
                }

                return Json(new { success = false, message = "Bill not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving bill details: " + ex.Message });
            }
        }

        // POST: Billing/DeleteBill
        [HttpPost]
        public IActionResult DeleteBill(int billId)
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
                    string sql = "DELETE FROM bills WHERE BillID = @BillID";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@BillID", billId);
                    int affected = cmd.ExecuteNonQuery();

                    if (affected > 0)
                    {
                        return Json(new { success = true, message = "Bill deleted successfully" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Bill not found" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting bill: " + ex.Message });
            }
        }

        // POST: Billing/EditBill
        [HttpPost]
        public IActionResult EditBill([FromBody] Bill model)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
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
                    string sql = @"UPDATE bills 
                                 SET Amount = @Amount, 
                                     DueDate = @DueDate, 
                                     Status = @Status, 
                                     Description = @Description,
                                     UpdatedAt = @UpdatedAt
                                 WHERE BillID = @BillID";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@BillID", model.BillID);
                    cmd.Parameters.AddWithValue("@Amount", model.Amount);
                    cmd.Parameters.AddWithValue("@DueDate", model.DueDate);
                    cmd.Parameters.AddWithValue("@Status", model.Status);
                    cmd.Parameters.AddWithValue("@Description", model.Description ?? "");
                    cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                    cmd.ExecuteNonQuery();

                    return Json(new { success = true, message = "Bill updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating bill: " + ex.Message });
            }
        }

        // GET: Billing/GetPayments
        public IActionResult GetPayments(int billId)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                var payments = new List<Payment>();
                Bill billDetails = null;

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    
                    // First get the bill details
                    string billSql = @"SELECT b.*, bt.Name as BillTypeName 
                                     FROM bills b 
                                     JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID 
                                     WHERE b.BillID = @BillID";
                    
                    MySqlCommand cmd = new MySqlCommand(billSql, conn);
                    cmd.Parameters.AddWithValue("@BillID", billId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            billDetails = new Bill
                            {
                                BillID = reader.GetInt32("BillID"),
                                Amount = reader.GetDecimal("Amount"),
                                DueDate = reader.GetDateTime("DueDate"),
                                Status = reader.GetString("Status"),
                                BillType = new BillType { Name = reader.GetString("BillTypeName") }
                            };
                        }
                    }

                    // Then get the payments
                    string paymentSql = @"SELECT p.*, u.Username as VerifierName 
                                        FROM payments p 
                                        LEFT JOIN users u ON p.VerifiedBy = u.UserID 
                                        WHERE p.BillID = @BillID 
                                        ORDER BY p.CreatedAt DESC";
                    
                    cmd = new MySqlCommand(paymentSql, conn);
                    cmd.Parameters.AddWithValue("@BillID", billId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            payments.Add(new Payment
                            {
                                PaymentID = reader.GetInt32("PaymentID"),
                                Amount = reader.GetDecimal("Amount"),
                                PaymentDate = reader.GetDateTime("PaymentDate"),
                                PaymentMethod = reader.GetString("PaymentMethod"),
                                ReferenceNumber = reader.GetString("ReferenceNumber"),
                                Status = reader.GetString("Status"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString("Notes"),
                                HasProofOfPayment = !reader.IsDBNull(reader.GetOrdinal("ProofOfPayment")),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                VerifiedAt = reader.IsDBNull(reader.GetOrdinal("VerifiedAt")) ? null : (DateTime?)reader.GetDateTime("VerifiedAt"),
                                Verifier = reader.IsDBNull(reader.GetOrdinal("VerifierName")) ? null : new UserModel { Username = reader.GetString("VerifierName") }
                            });
                        }
                    }
                }

                return Json(new { 
                    success = true, 
                    bill = billDetails,
                    payments = payments 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving payments: " + ex.Message });
            }
        }

        // GET: Billing/GetPendingPaymentsCount
        public IActionResult GetPendingPaymentsCount()
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
                    string sql = "SELECT COUNT(*) FROM payments WHERE Status = 'Pending'";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    return Json(new { success = true, count = count });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving pending payments count: " + ex.Message });
            }
        }

        // GET: Billing/GetPaymentHistory
        public IActionResult GetPaymentHistory(string status = "all", string method = "all", 
            string date = null, string search = null, int page = 1, int pageSize = 5)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                var payments = new List<Payment>();
                int totalCount = 0;

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // First get total count for pagination
                    string countSql = @"SELECT COUNT(*) 
                                      FROM payments p 
                                      JOIN bills b ON p.BillID = b.BillID 
                                      JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID 
                                      JOIN users u ON p.UserID = u.UserID 
                                      LEFT JOIN users v ON p.VerifiedBy = v.UserID 
                                      WHERE p.Status IN ('Verified', 'Rejected')";

                    string sql = @"SELECT p.*, 
                                        b.BillID, b.Amount as BillAmount, bt.Name as BillTypeName,
                                        u.Username, u.FirstName, u.LastName,
                                        v.Username as VerifierName,
                                        CASE WHEN p.ProofOfPayment IS NOT NULL THEN 1 ELSE 0 END as HasProofOfPayment
                                 FROM payments p 
                                 JOIN bills b ON p.BillID = b.BillID 
                                 JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID 
                                 JOIN users u ON p.UserID = u.UserID 
                                 LEFT JOIN users v ON p.VerifiedBy = v.UserID 
                                 WHERE p.Status IN ('Verified', 'Rejected')";

                    string conditions = "";

                    if (status != "all")
                    {
                        conditions += " AND p.Status = @Status";
                    }

                    if (method != "all")
                    {
                        conditions += " AND p.PaymentMethod = @Method";
                    }

                    if (!string.IsNullOrEmpty(date))
                    {
                        conditions += " AND DATE(p.VerifiedAt) = @Date";
                    }

                    if (!string.IsNullOrEmpty(search))
                    {
                        conditions += @" AND (u.FirstName LIKE @Search 
                                            OR u.LastName LIKE @Search 
                                            OR u.Username LIKE @Search)";
                    }

                    // Add conditions to count query
                    countSql += conditions;

                    // Get total count
                    MySqlCommand countCmd = new MySqlCommand(countSql, conn);
                    if (status != "all") countCmd.Parameters.AddWithValue("@Status", status);
                    if (method != "all") countCmd.Parameters.AddWithValue("@Method", method);
                    if (!string.IsNullOrEmpty(date)) countCmd.Parameters.AddWithValue("@Date", DateTime.Parse(date).Date);
                    if (!string.IsNullOrEmpty(search)) countCmd.Parameters.AddWithValue("@Search", $"%{search}%");

                    totalCount = Convert.ToInt32(countCmd.ExecuteScalar());

                    // Add conditions and pagination to main query
                    sql += conditions;
                    sql += " ORDER BY p.VerifiedAt DESC LIMIT @Offset, @PageSize";

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    if (status != "all") cmd.Parameters.AddWithValue("@Status", status);
                    if (method != "all") cmd.Parameters.AddWithValue("@Method", method);
                    if (!string.IsNullOrEmpty(date)) cmd.Parameters.AddWithValue("@Date", DateTime.Parse(date).Date);
                    if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@Search", $"%{search}%");
                    cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            payments.Add(new Payment
                            {
                                PaymentID = reader.GetInt32("PaymentID"),
                                Amount = reader.GetDecimal("Amount"),
                                PaymentDate = reader.GetDateTime("PaymentDate"),
                                PaymentMethod = reader.GetString("PaymentMethod"),
                                ReferenceNumber = reader.GetString("ReferenceNumber"),
                                Status = reader.GetString("Status"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString("Notes"),
                                HasProofOfPayment = reader.GetBoolean("HasProofOfPayment"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                VerifiedAt = reader.IsDBNull(reader.GetOrdinal("VerifiedAt")) ? null : (DateTime?)reader.GetDateTime("VerifiedAt"),
                                Bill = new Bill
                                {
                                    BillID = reader.GetInt32("BillID"),
                                    Amount = reader.GetDecimal("BillAmount"),
                                    BillType = new BillType { Name = reader.GetString("BillTypeName") }
                                },
                                User = new UserModel
                                {
                                    Username = reader.GetString("Username"),
                                    FirstName = reader.GetString("FirstName"),
                                    LastName = reader.GetString("LastName")
                                },
                                Verifier = new UserModel
                                {
                                    Username = reader.GetString("VerifierName")
                                }
                            });
                        }
                    }
                }

                return Json(new { 
                    success = true, 
                    payments = payments,
                    totalCount = totalCount,
                    currentPage = page,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving payment history: " + ex.Message });
            }
        }

        // GET: Billing/SearchHomeowners
        public IActionResult SearchHomeowners(string query)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                var homeowners = new List<dynamic>();
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT UserID, FirstName, LastName, Username 
                                 FROM users 
                                 WHERE Role = 'Homeowner' 
                                 AND (
                                     CONCAT(FirstName, ' ', LastName) LIKE @Query 
                                     OR UserID = CASE WHEN @IsNumber = 1 THEN @NumberQuery ELSE -1 END
                                     OR Username LIKE @Query
                                 )
                                 ORDER BY LastName, FirstName
                                 LIMIT 10";

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Query", $"%{query}%");
                    cmd.Parameters.AddWithValue("@IsNumber", int.TryParse(query, out int n));
                    cmd.Parameters.AddWithValue("@NumberQuery", n);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            homeowners.Add(new
                            {
                                userID = reader.GetInt32("UserID"),
                                firstName = reader.GetString("FirstName"),
                                lastName = reader.GetString("LastName"),
                                username = reader.GetString("Username")
                            });
                        }
                    }
                }

                return Json(new { success = true, homeowners = homeowners });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search error: {ex.Message}"); // Add logging
                return Json(new { success = false, message = "Error searching homeowners: " + ex.Message });
            }
        }

        // GET: Billing/GetHomeownerStats
        public IActionResult GetHomeownerStats(int homeownerId)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                var stats = new
                {
                    totalBilled = 0m,
                    totalPaid = 0m,
                    pendingAmount = 0m,
                    paymentRate = 0m
                };

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT 
                                   SUM(Amount) as TotalBilled,
                                   SUM(CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END) as TotalPaid,
                                   SUM(CASE WHEN Status IN ('Pending', 'Overdue') THEN Amount ELSE 0 END) as PendingAmount,
                                   COUNT(*) as TotalBills,
                                   SUM(CASE WHEN Status = 'Paid' AND PaymentDate <= DueDate THEN 1 ELSE 0 END) as OnTimeBills
                                 FROM bills 
                                 WHERE UserID = @UserID";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@UserID", homeownerId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            decimal totalBilled = reader.IsDBNull(reader.GetOrdinal("TotalBilled")) ? 0 : reader.GetDecimal("TotalBilled");
                            decimal totalPaid = reader.IsDBNull(reader.GetOrdinal("TotalPaid")) ? 0 : reader.GetDecimal("TotalPaid");
                            decimal pendingAmount = reader.IsDBNull(reader.GetOrdinal("PendingAmount")) ? 0 : reader.GetDecimal("PendingAmount");
                            int totalBills = reader.IsDBNull(reader.GetOrdinal("TotalBills")) ? 0 : reader.GetInt32("TotalBills");
                            int onTimeBills = reader.IsDBNull(reader.GetOrdinal("OnTimeBills")) ? 0 : reader.GetInt32("OnTimeBills");

                            stats = new
                            {
                                totalBilled = totalBilled,
                                totalPaid = totalPaid,
                                pendingAmount = pendingAmount,
                                paymentRate = totalBills > 0 ? (onTimeBills / (decimal)totalBills) * 100 : 0
                            };
                        }
                    }
                }

                return Json(new { success = true, stats = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving homeowner stats: " + ex.Message });
            }
        }

        // GET: Billing/GetHomeownerBillingHistory
        public IActionResult GetHomeownerBillingHistory(int homeownerId, string billType = "all", 
            string year = "all", string status = "all")
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                var history = new List<dynamic>();
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT b.*, bt.Name as BillTypeName,
                                p.PaymentID, p.Status as PaymentStatus, p.PaymentDate, 
                                p.PaymentMethod, p.ReferenceNumber, p.Amount as PaymentAmount,
                                p.CreatedAt as PaymentCreatedAt, p.VerifiedAt,
                                v.Username as VerifiedBy
                            FROM bills b
                            JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID
                            LEFT JOIN payments p ON b.BillID = p.BillID
                            LEFT JOIN users v ON p.VerifiedBy = v.UserID
                            WHERE b.UserID = @UserID";

                    if (billType != "all")
                    {
                        sql += " AND bt.Name = @BillType";
                    }

                    if (year != "all")
                    {
                        sql += " AND YEAR(b.CreatedAt) = @Year";
                    }

                    if (status != "all")
                    {
                        sql += " AND b.Status = @Status";
                    }

                    sql += " ORDER BY b.CreatedAt DESC, p.CreatedAt DESC";

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@UserID", homeownerId);
                    if (billType != "all") cmd.Parameters.AddWithValue("@BillType", billType);
                    if (year != "all") cmd.Parameters.AddWithValue("@Year", year);
                    if (status != "all") cmd.Parameters.AddWithValue("@Status", status);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            history.Add(new
                            {
                                billId = reader.GetInt32("BillID"),
                                billType = reader.GetString("BillTypeName"),
                                amount = reader.GetDecimal("Amount"),
                                dueDate = reader.GetDateTime("DueDate"),
                                status = reader.GetString("Status"),
                                createdAt = reader.GetDateTime("CreatedAt"),
                                paymentId = reader.IsDBNull(reader.GetOrdinal("PaymentID")) ? (int?)null : reader.GetInt32("PaymentID"),
                                paymentStatus = reader.IsDBNull(reader.GetOrdinal("PaymentStatus")) ? null : reader.GetString("PaymentStatus"),
                                paymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ? (DateTime?)null : reader.GetDateTime("PaymentDate"),
                                paymentMethod = reader.IsDBNull(reader.GetOrdinal("PaymentMethod")) ? null : reader.GetString("PaymentMethod"),
                                referenceNumber = reader.IsDBNull(reader.GetOrdinal("ReferenceNumber")) ? null : reader.GetString("ReferenceNumber"),
                                paymentAmount = reader.IsDBNull(reader.GetOrdinal("PaymentAmount")) ? (decimal?)null : reader.GetDecimal("PaymentAmount"),
                                verifiedBy = reader.IsDBNull(reader.GetOrdinal("VerifiedBy")) ? null : reader.GetString("VerifiedBy"),
                                verifiedAt = reader.IsDBNull(reader.GetOrdinal("VerifiedAt")) ? (DateTime?)null : reader.GetDateTime("VerifiedAt")
                            });
                        }
                    }

                    // Get all bill types for filtering
                    sql = "SELECT DISTINCT Name FROM bill_types ORDER BY Name";
                    cmd = new MySqlCommand(sql, conn);
                    var billTypes = new List<string>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            billTypes.Add(reader.GetString("Name"));
                        }
                    }

                    return Json(new { 
                        success = true, 
                        history = history,
                        billTypes = billTypes,
                        years = new[] { "2023", "2024", "2025" }
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving billing history: " + ex.Message });
            }
        }

        // GET: Billing/ExportHomeownerReport
        public IActionResult ExportHomeownerReport(int homeownerId)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Unauthorized();
            }

            try
            {
                // Implementation for exporting report to Excel
                // This would generate an Excel file with the homeowner's billing history
                // For now, we'll return a simple message
                return Content("Export functionality to be implemented");
            }
            catch (Exception ex)
            {
                return BadRequest("Error exporting report: " + ex.Message);
            }
        }

        // GET: Billing/GetPaymentMethodStats
        public IActionResult GetPaymentMethodStats()
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                var stats = new List<dynamic>();
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT 
                                    PaymentMethod,
                                    COUNT(*) as Count,
                                    SUM(Amount) as TotalAmount
                                 FROM payments
                                 WHERE Status = 'Verified'
                                 GROUP BY PaymentMethod
                                 ORDER BY Count DESC";
                    
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stats.Add(new
                            {
                                method = reader.GetString("PaymentMethod"),
                                count = reader.GetInt32("Count"),
                                amount = reader.GetDecimal("TotalAmount")
                            });
                        }
                    }
                }

                return Json(new { success = true, stats = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error getting payment method stats: " + ex.Message });
            }
        }

        // GET: Billing/GetRecentActivity
        public IActionResult GetRecentActivity(int page = 1, int pageSize = 5)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                var activities = new List<dynamic>();
                int totalCount = 0;

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Get total count
                    string countSql = @"SELECT COUNT(*) FROM (
                                        SELECT 'payment' as type, CreatedAt FROM payments
                                        UNION ALL
                                        SELECT 'bill' as type, CreatedAt FROM bills
                                        UNION ALL
                                        SELECT 'status_change' as type, UpdatedAt FROM bills WHERE UpdatedAt IS NOT NULL
                                      ) as combined";
                    
                    MySqlCommand countCmd = new MySqlCommand(countSql, conn);
                    totalCount = Convert.ToInt32(countCmd.ExecuteScalar());

                    // Get paginated activities
                    string sql = @"SELECT * FROM (
                                    SELECT 
                                        'payment' as type,
                                        p.PaymentID as ID,
                                        p.CreatedAt,
                                        CONCAT(u.FirstName, ' ', u.LastName) as UserName,
                                        p.Amount,
                                        p.Status as PaymentStatus,
                                        bt.Name as BillType
                                    FROM payments p
                                    JOIN users u ON p.UserID = u.UserID
                                    JOIN bills b ON p.BillID = b.BillID
                                    JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID
                                    
                                    UNION ALL
                                    
                                    SELECT 
                                        'bill' as type,
                                        b.BillID as ID,
                                        b.CreatedAt,
                                        CONCAT(u.FirstName, ' ', u.LastName) as UserName,
                                        b.Amount,
                                        b.Status as BillStatus,
                                        bt.Name as BillType
                                    FROM bills b
                                    JOIN users u ON b.UserID = u.UserID
                                    JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID
                                    
                                    UNION ALL
                                    
                                    SELECT 
                                        'status_change' as type,
                                        b.BillID as ID,
                                        b.UpdatedAt as CreatedAt,
                                        CONCAT(u.FirstName, ' ', u.LastName) as UserName,
                                        b.Amount,
                                        b.Status as NewStatus,
                                        bt.Name as BillType
                                    FROM bills b
                                    JOIN users u ON b.UserID = u.UserID
                                    JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID
                                    WHERE b.UpdatedAt IS NOT NULL
                                ) as combined
                                ORDER BY CreatedAt DESC
                                LIMIT @Offset, @PageSize";

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activities.Add(new
                            {
                                type = reader.GetString("type"),
                                id = reader.GetInt32("ID"),
                                createdAt = reader.GetDateTime("CreatedAt"),
                                userName = reader.GetString("UserName"),
                                amount = reader.GetDecimal("Amount"),
                                status = reader.GetString(reader.GetString("type") == "payment" ? "PaymentStatus" : "NewStatus"),
                                billType = reader.GetString("BillType")
                            });
                        }
                    }
                }

                return Json(new { 
                    success = true, 
                    activities = activities,
                    totalCount = totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error getting recent activity: " + ex.Message });
            }
        }

        // Helper methods
        private BillingDashboardViewModel GetDashboardData(int userId)
        {
            var dashboard = new BillingDashboardViewModel
            {
                RecentBills = new List<Bill>(),
                RecentPayments = new List<Payment>()
            };

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Get totals
                string sql = @"SELECT 
                               SUM(CASE WHEN Status = 'Pending' THEN Amount ELSE 0 END) as TotalDue,
                               SUM(CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END) as TotalPaid,
                               SUM(CASE WHEN Status = 'Overdue' THEN Amount ELSE 0 END) as TotalOverdue,
                               SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) as PendingCount,
                               SUM(CASE WHEN Status = 'Overdue' THEN 1 ELSE 0 END) as OverdueCount
                             FROM bills 
                             WHERE UserID = @UserID";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserID", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        dashboard.TotalDue = reader.IsDBNull(reader.GetOrdinal("TotalDue")) ? 0M : reader.GetDecimal("TotalDue");
                        dashboard.TotalPaid = reader.IsDBNull(reader.GetOrdinal("TotalPaid")) ? 0M : reader.GetDecimal("TotalPaid");
                        dashboard.TotalOverdue = reader.IsDBNull(reader.GetOrdinal("TotalOverdue")) ? 0M : reader.GetDecimal("TotalOverdue");
                        dashboard.PendingBillsCount = reader.IsDBNull(reader.GetOrdinal("PendingCount")) ? 0 : reader.GetInt32("PendingCount");
                        dashboard.OverdueBillsCount = reader.IsDBNull(reader.GetOrdinal("OverdueCount")) ? 0 : reader.GetInt32("OverdueCount");
                    }
                }

                // Get recent bills
                sql = @"SELECT b.*, bt.Name as BillTypeName 
                       FROM bills b 
                       JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID 
                       WHERE b.UserID = @UserID 
                       ORDER BY b.CreatedAt DESC LIMIT 5";
                cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserID", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dashboard.RecentBills.Add(new Bill
                        {
                            BillID = reader.GetInt32("BillID"),
                            Amount = reader.GetDecimal("Amount"),
                            DueDate = reader.GetDateTime("DueDate"),
                            Status = reader.GetString("Status"),
                            Description = reader.GetString("Description"),
                            BillType = new BillType { Name = reader.GetString("BillTypeName") }
                        });
                    }
                }

                // Get recent payments
                sql = @"SELECT p.*, b.Amount as BillAmount, bt.Name as BillTypeName 
                       FROM payments p 
                       JOIN bills b ON p.BillID = b.BillID 
                       JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID 
                       WHERE p.UserID = @UserID 
                       ORDER BY p.CreatedAt DESC LIMIT 5";
                cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserID", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dashboard.RecentPayments.Add(new Payment
                        {
                            PaymentID = reader.GetInt32("PaymentID"),
                            Amount = reader.GetDecimal("BillAmount"),
                            PaymentDate = reader.GetDateTime("PaymentDate"),
                            Status = reader.GetString("Status"),
                            PaymentMethod = reader.GetString("PaymentMethod"),
                            ReferenceNumber = reader.GetString("ReferenceNumber"),
                            Bill = new Bill
                            {
                                BillType = new BillType { Name = reader.GetString("BillTypeName") }
                            }
                        });
                    }
                }
            }

            return dashboard;
        }

        private Dictionary<string, decimal> GetAdminDashboardStats()
        {
            var stats = new Dictionary<string, decimal>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT 
                               SUM(CASE WHEN Status = 'Pending' THEN Amount ELSE 0 END) as TotalPending,
                               SUM(CASE WHEN Status = 'Paid' THEN Amount ELSE 0 END) as TotalCollected,
                               SUM(CASE WHEN Status = 'Overdue' THEN Amount ELSE 0 END) as TotalOverdue
                             FROM bills";
                MySqlCommand cmd = new MySqlCommand(sql, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        stats["TotalPending"] = reader.IsDBNull(reader.GetOrdinal("TotalPending")) ? 0M : reader.GetDecimal("TotalPending");
                        stats["TotalCollected"] = reader.IsDBNull(reader.GetOrdinal("TotalCollected")) ? 0M : reader.GetDecimal("TotalCollected");
                        stats["TotalOverdue"] = reader.IsDBNull(reader.GetOrdinal("TotalOverdue")) ? 0M : reader.GetDecimal("TotalOverdue");
                    }
                    else
                    {
                        // Initialize with default values if no data is found
                        stats["TotalPending"] = 0M;
                        stats["TotalCollected"] = 0M;
                        stats["TotalOverdue"] = 0M;
                    }
                }
            }

            return stats;
        }

        private List<Bill> GetAllBills()
        {
            var bills = new List<Bill>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"SELECT b.*, bt.Name as BillTypeName, u.Username 
                             FROM bills b 
                             JOIN bill_types bt ON b.BillTypeID = bt.BillTypeID 
                             JOIN users u ON b.UserID = u.UserID 
                             ORDER BY b.CreatedAt DESC";
                MySqlCommand cmd = new MySqlCommand(sql, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bills.Add(new Bill
                        {
                            BillID = reader.GetInt32("BillID"),
                            Amount = reader.GetDecimal("Amount"),
                            DueDate = reader.GetDateTime("DueDate"),
                            Status = reader.GetString("Status"),
                            Description = reader.GetString("Description"),
                            CreatedAt = reader.GetDateTime("CreatedAt"),
                            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : (DateTime?)reader.GetDateTime("UpdatedAt"),
                            BillType = new BillType { Name = reader.GetString("BillTypeName") },
                            User = new UserModel { Username = reader.GetString("Username") }
                        });
                    }
                }
            }

            return bills;
        }

        private List<BillType> GetBillTypes()
        {
            var types = new List<BillType>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM bill_types ORDER BY Name";
                MySqlCommand cmd = new MySqlCommand(sql, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        types.Add(new BillType
                        {
                            BillTypeID = reader.GetInt32("BillTypeID"),
                            Name = reader.GetString("Name"),
                            Description = reader.GetString("Description"),
                            IsRecurring = reader.GetBoolean("IsRecurring")
                        });
                    }
                }
            }

            return types;
        }

        private List<UserModel> GetUsers()
        {
            var users = new List<UserModel>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT UserID, Username, FirstName, LastName FROM users WHERE Role = 'Homeowner' ORDER BY Username";
                MySqlCommand cmd = new MySqlCommand(sql, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new UserModel
                        {
                            UserID = reader.GetInt32("UserID"),
                            Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? string.Empty : reader.GetString("Username"),
                            FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? string.Empty : reader.GetString("FirstName"),
                            LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? string.Empty : reader.GetString("LastName")
                        });
                    }
                }
            }

            return users;
        }
    }
} 