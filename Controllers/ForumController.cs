using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System.Collections.Generic;
using System;
using System.IO;
using System.Security.Claims; // ✅ Required for user identity logging

namespace homeowner.Controllers
{
    public class ForumController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";

        // GET: /Forum/
        public IActionResult Index()
        {
            // ✅ Get the UserID from session
            string currentUserId = HttpContext.Session.GetString("UserID");

            // ✅ Log the user ID in the VS Code terminal
            Console.WriteLine($"[LOG] Current Logged-in User ID: {currentUserId ?? "No user logged in"}");

            // ✅ Pass it to the ViewBag for debugging in the frontend
            ViewBag.CurrentUserId = currentUserId;

            List<ForumPostModel> posts = new List<ForumPostModel>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
    SELECT fp.*, u.Username, 
           (SELECT COUNT(*) FROM forum_post_votes WHERE PostID = fp.PostID AND VoteValue = 1) AS Upvotes,
           (SELECT COUNT(*) FROM forum_post_votes WHERE PostID = fp.PostID AND VoteValue = -1) AS Downvotes
    FROM forum_posts fp 
    JOIN users u ON fp.UserID = u.UserID 
    ORDER BY fp.CreatedAt DESC";



                MySqlCommand cmd = new MySqlCommand(sql, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    posts.Add(new ForumPostModel
                    {
                        PostID = reader.GetInt32("PostID"),
                        UserID = reader.IsDBNull(reader.GetOrdinal("UserID")) ? (int?)null : reader.GetInt32("UserID"),
                        Username = reader.GetString("Username"),
                        Title = reader.GetString("Title"),
                        Content = reader.GetString("Content"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        Image = reader.IsDBNull(reader.GetOrdinal("Image")) ? null : (byte[])reader["Image"],
                        Upvotes = reader.GetInt32("Upvotes"),   // Get upvote count
                        Downvotes = reader.GetInt32("Downvotes") // Get downvote count
                    });
                }

                reader.Close();
            }
            return View("~/Views/Homeowner/Forum.cshtml", posts);
        }


        [HttpGet]
        public IActionResult GetPostVotes(int postID)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT COALESCE(SUM(VoteValue), 0) FROM forum_post_votes WHERE PostID=@PostID";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@PostID", postID);
                object result = cmd.ExecuteScalar();
                int voteCount = result != DBNull.Value ? Convert.ToInt32(result) : 0;

                return Json(new { success = true, voteCount = voteCount });
            }
        }



        public IActionResult Details(int id)
        {
            ForumPostModel post = null;
            List<ForumCommentModel> comments = new List<ForumCommentModel>();
            int? currentUserId = null; // Store the current user's ID

            if (HttpContext.Session.GetString("UserID") != null)
            {
                currentUserId = int.Parse(HttpContext.Session.GetString("UserID"));
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Get the forum post.
                string sqlPost = "SELECT * FROM forum_posts WHERE PostID = @PostID";
                using (MySqlCommand cmdPost = new MySqlCommand(sqlPost, conn))
                {
                    cmdPost.Parameters.AddWithValue("@PostID", id);
                    using (var reader = cmdPost.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            post = new ForumPostModel
                            {
                                PostID = reader.GetInt32("PostID"),
                                UserID = reader.IsDBNull(reader.GetOrdinal("UserID")) ? (int?)null : reader.GetInt32("UserID"),
                                Title = reader.GetString("Title"),
                                Content = reader.GetString("Content"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                Image = reader.IsDBNull(reader.GetOrdinal("Image")) ? null : (byte[])reader["Image"]
                            };
                        }
                    }
                }

                // Get the comments for the post.
                string sqlComments = @"
            SELECT c.*, 
            (SELECT COUNT(*) FROM forum_comment_votes WHERE CommentID = c.CommentID AND VoteValue = 1) AS Upvotes,
            (SELECT COUNT(*) FROM forum_comment_votes WHERE CommentID = c.CommentID AND VoteValue = -1) AS Downvotes
            FROM forum_comments c 
            WHERE c.PostID = @PostID
            ORDER BY c.CreatedAt ASC";

                using (MySqlCommand cmdComments = new MySqlCommand(sqlComments, conn))
                {
                    cmdComments.Parameters.AddWithValue("@PostID", id);
                    using (var readerComments = cmdComments.ExecuteReader())
                    {
                        while (readerComments.Read())
                        {
                            comments.Add(new ForumCommentModel
                            {
                                CommentID = readerComments.GetInt32("CommentID"),
                                PostID = readerComments.GetInt32("PostID"),
                                UserID = readerComments.GetInt32("UserID"),
                                Content = readerComments.GetString("Content"),
                                CreatedAt = readerComments.GetDateTime("CreatedAt"),
                                Image = readerComments.IsDBNull(readerComments.GetOrdinal("Image")) ? null : (byte[])readerComments["Image"],
                                Upvotes = readerComments.GetInt32("Upvotes"),
                                Downvotes = readerComments.GetInt32("Downvotes")
                            });
                        }
                    }
                }
            }

            if (post == null)
            {
                return NotFound();
            }

            ViewBag.Comments = comments;
            ViewBag.CurrentUserId = currentUserId; // Pass UserID to the view
            return View(post);
        }





        // GET: /Forum/CreatePost
        public IActionResult CreatePost()
        {
            return View();
        }

        // POST: /Forum/CreatePost
        [HttpPost]
        public IActionResult CreatePost(ForumPostModel model, IFormFile imageFile)
        {
            // Get current user from session.
            string userIdStr = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Account");
            }
            int userID = int.Parse(userIdStr);

            byte[] imageData = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    imageFile.CopyTo(ms);
                    imageData = ms.ToArray();
                }
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "INSERT INTO forum_posts (UserID, Title, Content, Image) VALUES (@UserID, @Title, @Content, @Image)";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@UserID", userID);
                cmd.Parameters.AddWithValue("@Title", model.Title);
                cmd.Parameters.AddWithValue("@Content", model.Content);
                cmd.Parameters.AddWithValue("@Image", imageData ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        public IActionResult EditPost(int id)
        {
            string currentUserId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
            {
                return RedirectToAction("Index");
            }

            ForumPostModel post = null;
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM forum_posts WHERE PostID = @PostID";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@PostID", id);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    post = new ForumPostModel
                    {
                        PostID = reader.GetInt32("PostID"),
                        UserID = reader.IsDBNull(reader.GetOrdinal("UserID")) ? (int?)null : reader.GetInt32("UserID"),
                        Title = reader.GetString("Title"),
                        Content = reader.GetString("Content"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        Image = reader.IsDBNull(reader.GetOrdinal("Image")) ? null : (byte[])reader["Image"]
                    };
                }
                reader.Close();
            }

            if (post == null || post.UserID != userId)
            {
                return RedirectToAction("Index");
            }

            return View(post);
        }


        [HttpPost]
        public IActionResult EditPost(ForumPostModel model, IFormFile imageFile)
        {
            string currentUserId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
            {
                Console.WriteLine("Unauthorized access attempt - No valid UserID in session.");
                return Json(new { success = false, message = "Unauthorized access" });
            }

            Console.WriteLine($"EditPost Called - PostID: {model.PostID}, Title: {model.Title}, Content: {model.Content}");

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Check if the post exists and belongs to the user
                    string sqlCheck = "SELECT UserID FROM forum_posts WHERE PostID = @PostID";
                    MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, conn);
                    cmdCheck.Parameters.AddWithValue("@PostID", model.PostID);

                    object ownerObj = cmdCheck.ExecuteScalar();
                    if (ownerObj == null)
                    {
                        Console.WriteLine($"Post with ID {model.PostID} not found.");
                        return Json(new { success = false, message = "Post not found." });
                    }

                    int ownerID = Convert.ToInt32(ownerObj);
                    if (ownerID != userId)
                    {
                        Console.WriteLine($"Unauthorized edit attempt. Owner ID: {ownerID}, User ID: {userId}");
                        return Json(new { success = false, message = "You can only edit your own posts." });
                    }

                    // Process image if uploaded
                    byte[] imageData = null;
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            imageFile.CopyTo(ms);
                            imageData = ms.ToArray();
                        }
                        Console.WriteLine("Image uploaded and processed.");
                    }

                    string sql = "UPDATE forum_posts SET Title=@Title, Content=@Content" +
                                 (imageData != null ? ", Image=@Image" : "") +
                                 " WHERE PostID=@PostID AND UserID=@UserID";

                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Title", model.Title);
                    cmd.Parameters.AddWithValue("@Content", model.Content);
                    cmd.Parameters.AddWithValue("@PostID", model.PostID);
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    if (imageData != null)
                    {
                        cmd.Parameters.AddWithValue("@Image", imageData);
                    }

                    Console.WriteLine("Executing SQL Update: " + sql);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    Console.WriteLine($"Rows affected: {rowsAffected}");

                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "Post updated successfully!" });
                    }
                    else
                    {
                        Console.WriteLine("Update failed: No rows affected.");
                        return Json(new { success = false, message = "Update failed." });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating post: {ex.Message}");
                    return Json(new { success = false, message = "An error occurred while updating the post." });
                }
            }
        }




        [HttpPost]
        public IActionResult DeletePost(int id)
        {
            // Retrieve the current user ID from the session
            string currentUserId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Check if the post belongs to the user
                    string sqlCheck = "SELECT UserID FROM forum_posts WHERE PostID = @PostID";
                    using (MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@PostID", id);
                        object result = cmdCheck.ExecuteScalar();

                        if (result == null)
                        {
                            return Json(new { success = false, message = "Post not found." });
                        }

                        int postUserId = Convert.ToInt32(result);
                        if (postUserId != userId)
                        {
                            return Json(new { success = false, message = "Unauthorized" });
                        }
                    }

                    // Delete the post
                    string sqlDelete = "DELETE FROM forum_posts WHERE PostID = @PostID";
                    using (MySqlCommand cmdDelete = new MySqlCommand(sqlDelete, conn))
                    {
                        cmdDelete.Parameters.AddWithValue("@PostID", id);
                        cmdDelete.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Post deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while deleting the post.", error = ex.Message });
            }
        }




        // POST: /Forum/AddComment
        [HttpPost]
        public IActionResult AddComment(ForumCommentModel comment, IFormFile imageFile)
        {
            string userIdStr = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Json(new { success = false, message = "User not logged in" });
            }
            comment.UserID = int.Parse(userIdStr);

            byte[] imageData = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    imageFile.CopyTo(ms);
                    imageData = ms.ToArray();
                }
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "INSERT INTO forum_comments (PostID, UserID, Content, Image) VALUES (@PostID, @UserID, @Content, @Image)";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@PostID", comment.PostID);
                cmd.Parameters.AddWithValue("@UserID", comment.UserID);
                cmd.Parameters.AddWithValue("@Content", comment.Content);
                cmd.Parameters.AddWithValue("@Image", imageData ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            return Json(new { success = true, message = "Comment added successfully." });
        }

        // GET: /Forum/EditComment/5
        public IActionResult EditComment(int id)
        {
            ForumCommentModel comment = null;
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM forum_comments WHERE CommentID = @CommentID";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@CommentID", id);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    comment = new ForumCommentModel
                    {
                        CommentID = reader.GetInt32("CommentID"),
                        PostID = reader.GetInt32("PostID"),
                        UserID = reader.GetInt32("UserID"),
                        Content = reader.GetString("Content"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        Image = reader.IsDBNull(reader.GetOrdinal("Image")) ? null : (byte[])reader["Image"]
                    };
                }
                reader.Close();
            }
            // Only allow editing if current user owns the comment.
            string currentUserId = HttpContext.Session.GetString("UserID");
            if (comment == null || comment.UserID.ToString() != currentUserId)
            {
                return RedirectToAction("Details", new { id = comment?.PostID });
            }
            return View(comment);
        }

        // POST: /Forum/EditComment
        [HttpPost]
        public JsonResult EditComment(int CommentID, string Content)
        {
            try
            {
                // Validate inputs
                if (CommentID <= 0 || string.IsNullOrWhiteSpace(Content))
                {
                    return Json(new { success = false, message = "Invalid input data." });
                }

                // Get current user ID from session
                string userIdStr = HttpContext.Session.GetString("UserID");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    return Json(new { success = false, message = "Unauthorized: User not logged in." });
                }

                int userID = int.Parse(userIdStr);

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Verify comment exists and belongs to the user
                    string sqlCheck = "SELECT UserID FROM forum_comments WHERE CommentID = @CommentID";
                    using (MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@CommentID", CommentID);
                        var reader = cmdCheck.ExecuteReader();

                        if (!reader.Read() || reader.GetInt32("UserID") != userID)
                        {
                            reader.Close();
                            return Json(new { success = false, message = "Unauthorized or comment not found." });
                        }
                        reader.Close();
                    }

                    // Update the comment
                    string sqlUpdate = "UPDATE forum_comments SET Content = @Content WHERE CommentID = @CommentID";
                    using (MySqlCommand cmdUpdate = new MySqlCommand(sqlUpdate, conn))
                    {
                        cmdUpdate.Parameters.AddWithValue("@Content", Content);
                        cmdUpdate.Parameters.AddWithValue("@CommentID", CommentID);
                        int rowsAffected = cmdUpdate.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Failed to update comment." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }


        // POST: /Forum/DeleteComment/5
        [HttpPost]
        public IActionResult DeleteComment(int id)
        {
            ForumCommentModel comment = null;
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sqlCheck = "SELECT * FROM forum_comments WHERE CommentID = @CommentID";
                MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, conn);
                cmdCheck.Parameters.AddWithValue("@CommentID", id);
                var reader = cmdCheck.ExecuteReader();
                if (reader.Read())
                {
                    comment = new ForumCommentModel
                    {
                        CommentID = reader.GetInt32("CommentID"),
                        PostID = reader.GetInt32("PostID"),
                        UserID = reader.GetInt32("UserID")
                    };
                }
                reader.Close();
            }
            string currentUserId = HttpContext.Session.GetString("UserID");
            if (comment == null || comment.UserID.ToString() != currentUserId)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM forum_comments WHERE CommentID = @CommentID";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@CommentID", id);
                cmd.ExecuteNonQuery();
            }
            return Json(new { success = true, message = "Comment deleted." });
        }

        // POST: /Forum/VotePost
        [HttpPost]
        public IActionResult VotePost(int postID, sbyte voteValue)
        {
            string userIdStr = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userIdStr))
                return Json(new { success = false, message = "User not logged in" });
            int userID = int.Parse(userIdStr);

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // Check if a vote already exists.
                string checkSql = "SELECT VoteValue FROM forum_post_votes WHERE UserID=@UserID AND PostID=@PostID";
                MySqlCommand checkCmd = new MySqlCommand(checkSql, conn);
                checkCmd.Parameters.AddWithValue("@UserID", userID);
                checkCmd.Parameters.AddWithValue("@PostID", postID);
                object existingObj = checkCmd.ExecuteScalar();
                if (existingObj != null)
                {
                    sbyte existingVote = Convert.ToSByte(existingObj);
                    if (existingVote == voteValue)
                    {
                        // If the same vote exists, remove it.
                        string deleteSql = "DELETE FROM forum_post_votes WHERE UserID=@UserID AND PostID=@PostID";
                        MySqlCommand deleteCmd = new MySqlCommand(deleteSql, conn);
                        deleteCmd.Parameters.AddWithValue("@UserID", userID);
                        deleteCmd.Parameters.AddWithValue("@PostID", postID);
                        deleteCmd.ExecuteNonQuery();
                        return Json(new { success = true, message = "Vote removed." });
                    }
                    else
                    {
                        // Update the vote.
                        string updateSql = "UPDATE forum_post_votes SET VoteValue=@VoteValue WHERE UserID=@UserID AND PostID=@PostID";
                        MySqlCommand updateCmd = new MySqlCommand(updateSql, conn);
                        updateCmd.Parameters.AddWithValue("@VoteValue", voteValue);
                        updateCmd.Parameters.AddWithValue("@UserID", userID);
                        updateCmd.Parameters.AddWithValue("@PostID", postID);
                        updateCmd.ExecuteNonQuery();
                        return Json(new { success = true, message = "Vote updated." });
                    }
                }
                else
                {
                    // Insert a new vote.
                    string insertSql = "INSERT INTO forum_post_votes (UserID, PostID, VoteValue) VALUES (@UserID, @PostID, @VoteValue)";
                    MySqlCommand insertCmd = new MySqlCommand(insertSql, conn);
                    insertCmd.Parameters.AddWithValue("@UserID", userID);
                    insertCmd.Parameters.AddWithValue("@PostID", postID);
                    insertCmd.Parameters.AddWithValue("@VoteValue", voteValue);
                    insertCmd.ExecuteNonQuery();
                    return Json(new { success = true, message = "Vote recorded." });
                }
            }
        }

        // POST: /Forum/VoteComment
        [HttpPost]
        public IActionResult VoteComment(int commentID, sbyte voteValue)
        {
            string userIdStr = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userIdStr))
                return Json(new { success = false, message = "User not logged in" });

            int userID = int.Parse(userIdStr);
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string checkSql = "SELECT VoteValue FROM forum_comment_votes WHERE UserID=@UserID AND CommentID=@CommentID";
                MySqlCommand checkCmd = new MySqlCommand(checkSql, conn);
                checkCmd.Parameters.AddWithValue("@UserID", userID);
                checkCmd.Parameters.AddWithValue("@CommentID", commentID);
                object existingObj = checkCmd.ExecuteScalar();

                if (existingObj != null)
                {
                    sbyte existingVote = Convert.ToSByte(existingObj);
                    if (existingVote == voteValue)
                    {
                        string deleteSql = "DELETE FROM forum_comment_votes WHERE UserID=@UserID AND CommentID=@CommentID";
                        MySqlCommand deleteCmd = new MySqlCommand(deleteSql, conn);
                        deleteCmd.Parameters.AddWithValue("@UserID", userID);
                        deleteCmd.Parameters.AddWithValue("@CommentID", commentID);
                        deleteCmd.ExecuteNonQuery();
                        return Json(new { success = true, message = "Vote removed." });
                    }
                    else
                    {
                        string updateSql = "UPDATE forum_comment_votes SET VoteValue=@VoteValue WHERE UserID=@UserID AND CommentID=@CommentID";
                        MySqlCommand updateCmd = new MySqlCommand(updateSql, conn);
                        updateCmd.Parameters.AddWithValue("@VoteValue", voteValue);
                        updateCmd.Parameters.AddWithValue("@UserID", userID);
                        updateCmd.Parameters.AddWithValue("@CommentID", commentID);
                        updateCmd.ExecuteNonQuery();
                        return Json(new { success = true, message = "Vote updated." });
                    }
                }
                else
                {
                    string insertSql = "INSERT INTO forum_comment_votes (UserID, CommentID, VoteValue) VALUES (@UserID, @CommentID, @VoteValue)";
                    MySqlCommand insertCmd = new MySqlCommand(insertSql, conn);
                    insertCmd.Parameters.AddWithValue("@UserID", userID);
                    insertCmd.Parameters.AddWithValue("@CommentID", commentID);
                    insertCmd.Parameters.AddWithValue("@VoteValue", voteValue);
                    insertCmd.ExecuteNonQuery();
                    return Json(new { success = true, message = "Vote recorded." });
                }
            }
        }



    }
}
