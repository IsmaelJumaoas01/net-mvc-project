using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System;
using System.Collections.Generic;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace homeowner.Controllers
{
    public class PollController : Controller
    {
        private readonly string _connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";

        public IActionResult Index()
        {
            string role = HttpContext.Session.GetString("Role") ?? "";
            ViewBag.IsAdmin = role == "Administrator";
            ViewBag.IsStaff = role == "Staff";

            List<PollModel> polls = new List<PollModel>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string sql = @"SELECT p.*, u.Username as CreatedByUsername,
                              (SELECT COUNT(*) FROM poll_responses WHERE PollID = p.PollID) as ResponseCount
                              FROM polls p
                              JOIN USERS u ON p.CreatedBy = u.UserID
                              ORDER BY p.CreatedAt DESC";
                polls = connection.Query<PollModel>(sql).ToList();

                // Get options for each poll
                foreach (var poll in polls)
                {
                    string optionsSql = @"SELECT po.*, 
                                         (SELECT COUNT(*) FROM poll_responses WHERE OptionID = po.OptionID) as VoteCount
                                         FROM poll_options po 
                                         WHERE po.PollID = @PollID";
                    poll.Options = connection.Query<PollOptionModel>(optionsSql, new { PollID = poll.PollID }).ToList();

                    // Calculate percentages
                    int totalVotes = poll.Options.Sum(o => o.VoteCount);
                    foreach (var option in poll.Options)
                    {
                        option.Percentage = totalVotes > 0 ? (double)option.VoteCount / totalVotes * 100 : 0;
                    }
                }

                // Get user's responses if logged in
                string userIdStr = HttpContext.Session.GetString("UserID");
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    int userId = int.Parse(userIdStr);
                    string voteSql = "SELECT PollID FROM poll_responses WHERE UserID = @UserID";
                    var userVotes = connection.Query<int>(voteSql, new { UserID = userId }).ToList();
                    foreach (var poll in polls)
                    {
                        poll.HasVoted = userVotes.Contains(poll.PollID);
                    }
                }
            }

            return View(polls);
        }

        [HttpPost]
        public IActionResult Vote(int pollId, int optionId)
        {
            string userIdStr = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Json(new { success = false, message = "Please log in to vote." });
            }

            int userId = int.Parse(userIdStr);

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check if user has already voted
                        var existingVoteSql = "SELECT OptionID FROM poll_responses WHERE PollID = @PollID AND UserID = @UserID";
                        var existingVote = connection.QueryFirstOrDefault<int?>(existingVoteSql, new { PollID = pollId, UserID = userId });

                        if (existingVote.HasValue)
                        {
                            if (existingVote.Value == optionId)
                            {
                                // Remove vote if clicking the same option
                                connection.Execute(
                                    "DELETE FROM poll_responses WHERE PollID = @PollID AND UserID = @UserID",
                                    new { PollID = pollId, UserID = userId },
                                    transaction
                                );
                                transaction.Commit();
                                return Json(new { success = true, message = "Vote removed successfully." });
                            }
                            else
                            {
                                // Change vote to new option
                                connection.Execute(
                                    "UPDATE poll_responses SET OptionID = @OptionID WHERE PollID = @PollID AND UserID = @UserID",
                                    new { OptionID = optionId, PollID = pollId, UserID = userId },
                                    transaction
                                );
                                transaction.Commit();
                                return Json(new { success = true, message = "Vote changed successfully." });
                            }
                        }
                        else
                        {
                            // Add new vote
                            connection.Execute(
                                "INSERT INTO poll_responses (PollID, UserID, OptionID) VALUES (@PollID, @UserID, @OptionID)",
                                new { PollID = pollId, UserID = userId, OptionID = optionId },
                                transaction
                            );
                            transaction.Commit();
                            return Json(new { success = true, message = "Vote recorded successfully." });
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
                    }
                }
            }
        }

        public IActionResult Create()
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Create([FromBody] PollModel poll)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            if (poll.Options == null || poll.Options.Count < 2)
            {
                return Json(new { success = false, message = "Please provide at least two options." });
            }

            string userIdStr = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insert poll
                        var pollSql = @"
                            INSERT INTO polls (Title, Description, StartDate, EndDate, CreatedBy, CreatedAt) 
                            VALUES (@Title, @Description, @StartDate, @EndDate, @CreatedBy, NOW());
                            SELECT LAST_INSERT_ID();";
                        
                        var pollId = connection.ExecuteScalar<int>(pollSql, new { 
                            poll.Title, 
                            poll.Description,
                            poll.StartDate,
                            poll.EndDate,
                            CreatedBy = int.Parse(userIdStr)
                        }, transaction);

                        // Insert options
                        var optionSql = "INSERT INTO poll_options (PollID, OptionText) VALUES (@PollID, @OptionText)";
                        foreach (var option in poll.Options)
                        {
                            if (!string.IsNullOrWhiteSpace(option.OptionText))
                            {
                                connection.Execute(optionSql, new { PollID = pollId, option.OptionText }, transaction);
                            }
                        }

                        transaction.Commit();
                        return Json(new { success = true, message = "Poll created successfully" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
                    }
                }
            }
        }

        [HttpPost]
        public IActionResult DeletePoll(int pollId)
        {
            string role = HttpContext.Session.GetString("Role");
            if (role != "Administrator" && role != "Staff")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Delete responses first
                        connection.Execute("DELETE FROM poll_responses WHERE PollID = @PollID", new { PollID = pollId }, transaction);
                        
                        // Delete options
                        connection.Execute("DELETE FROM poll_options WHERE PollID = @PollID", new { PollID = pollId }, transaction);
                        
                        // Delete poll
                        connection.Execute("DELETE FROM polls WHERE PollID = @PollID", new { PollID = pollId }, transaction);

                        transaction.Commit();
                        return Json(new { success = true, message = "Poll deleted successfully." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
                    }
                }
            }
        }

        [HttpGet]
        public IActionResult GetResults(int pollId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                var results = connection.Query<PollOptionModel>(@"
                    SELECT po.OptionID, po.OptionText, COUNT(pr.ResponseID) as VoteCount
                    FROM poll_options po
                    LEFT JOIN poll_responses pr ON po.OptionID = pr.OptionID
                    WHERE po.PollID = @PollID
                    GROUP BY po.OptionID, po.OptionText", new { PollID = pollId });

                return Json(new { success = true, results = results });
            }
        }
    }
} 