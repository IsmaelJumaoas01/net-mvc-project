using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System.Collections.Generic;
using System;
using System.IO;
using System.Security.Claims; // ✅ Required for user identity logging
using Dapper;

namespace homeowner.Controllers
{
    public class ForumController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";

        // GET: /Forum/
        public IActionResult Index()
        {
            string currentUserId = HttpContext.Session.GetString("UserID");
            Console.WriteLine($"[LOG] Current Logged-in User ID: {currentUserId ?? "No user logged in"}");
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
                        Upvotes = reader.GetInt32("Upvotes"),
                        Downvotes = reader.GetInt32("Downvotes")
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
            string userIdStr = HttpContext.Session.GetString("UserID");
            ViewBag.CurrentUserId = userIdStr;

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Get post details
                string postSql = @"
                    SELECT p.*, u.Username,
                           (SELECT COUNT(*) FROM forum_post_votes WHERE PostID = p.PostID AND VoteValue = 1) as Upvotes,
                           (SELECT COUNT(*) FROM forum_post_votes WHERE PostID = p.PostID AND VoteValue = -1) as Downvotes,
                           CASE WHEN EXISTS (SELECT 1 FROM forum_post_votes WHERE PostID = p.PostID AND UserID = @UserID AND VoteValue = 1) THEN 1 ELSE 0 END as HasUpvoted,
                           CASE WHEN EXISTS (SELECT 1 FROM forum_post_votes WHERE PostID = p.PostID AND UserID = @UserID AND VoteValue = -1) THEN 1 ELSE 0 END as HasDownvoted
                    FROM forum_posts p
                    JOIN users u ON p.UserID = u.UserID
                    WHERE p.PostID = @PostID";

                var post = connection.QueryFirstOrDefault<ForumPostModel>(postSql, new { PostID = id, UserID = userIdStr });
                if (post == null)
                {
                    return NotFound();
                }

                // Get comments
                string commentSql = @"
                    SELECT c.*, u.Username,
                           (SELECT COUNT(*) FROM forum_comment_votes WHERE CommentID = c.CommentID AND VoteValue = 1) as Upvotes,
                           (SELECT COUNT(*) FROM forum_comment_votes WHERE CommentID = c.CommentID AND VoteValue = -1) as Downvotes,
                           CASE WHEN EXISTS (SELECT 1 FROM forum_comment_votes WHERE CommentID = c.CommentID AND UserID = @UserID AND VoteValue = 1) THEN 1 ELSE 0 END as HasUpvoted,
                           CASE WHEN EXISTS (SELECT 1 FROM forum_comment_votes WHERE CommentID = c.CommentID AND UserID = @UserID AND VoteValue = -1) THEN 1 ELSE 0 END as HasDownvoted
                    FROM forum_comments c
                    JOIN users u ON c.UserID = u.UserID
                    WHERE c.PostID = @PostID
                    ORDER BY c.CreatedAt DESC";

                var comments = connection.Query<ForumCommentModel>(commentSql, new { PostID = id, UserID = userIdStr }).ToList();
                ViewBag.Comments = comments;

                return View(post);
            }
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
                return Json(new { success = false, message = "Unauthorized access" });
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Check if post exists & belongs to user
                    string sqlCheck = "SELECT UserID, Image FROM forum_posts WHERE PostID = @PostID";
                    byte[] existingImage = null;
                    using (MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@PostID", model.PostID);
                        using (var reader = cmdCheck.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int ownerID = reader.GetInt32("UserID");
                                if (ownerID != userId)
                                {
                                    return Json(new { success = false, message = "You can only edit your own posts." });
                                }
                                if (!reader.IsDBNull(reader.GetOrdinal("Image")))
                                {
                                    existingImage = (byte[])reader["Image"];
                                }
                            }
                            else
                            {
                                return Json(new { success = false, message = "Post not found." });
                            }
                        }
                    }

                    // Process image upload
                    byte[] imageData = null;
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            imageFile.CopyTo(ms);
                            imageData = ms.ToArray();
                        }
                    }

                    // Construct SQL query dynamically
                    string sql = "UPDATE forum_posts SET Title=@Title, Content=@Content";
                    if (imageData != null)
                    {
                        sql += ", Image=@Image";
                    }
                    sql += " WHERE PostID=@PostID AND UserID=@UserID";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Title", model.Title);
                        cmd.Parameters.AddWithValue("@Content", model.Content);
                        cmd.Parameters.AddWithValue("@PostID", model.PostID);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        if (imageData != null)
                        {
                            cmd.Parameters.AddWithValue("@Image", imageData);
                        }

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            string imageUrl = null;
                            if (imageData != null)
                            {
                                imageUrl = "data:image/png;base64," + Convert.ToBase64String(imageData);
                            }
                            else if (existingImage != null)
                            {
                                imageUrl = "data:image/png;base64," + Convert.ToBase64String(existingImage);
                            }
                            return Json(new { success = true, message = "Post updated successfully!", imageUrl = imageUrl });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Update failed." });
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Packets larger than max_allowed_packet"))
                    {
                        return Json(new { success = false, message = "The image is too large to upload. Please choose a smaller image." });
                    }
                    return Json(new { success = false, message = "An error occurred while updating the post." });
                }
            }
        }


        [HttpPost]
        public IActionResult DeletePostImage([FromForm] int postId)
        {
            string currentUserId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
            {
                return Json(new { success = false, message = "Unauthorized access" });
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Check if the post exists and belongs to the user
                    string sqlCheck = "SELECT UserID FROM forum_posts WHERE PostID = @PostID";
                    MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, conn);
                    cmdCheck.Parameters.AddWithValue("@PostID", postId);
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

                    // Delete the image
                    string sql = "UPDATE forum_posts SET Image = NULL WHERE PostID = @PostID AND UserID = @UserID";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@PostID", postId);
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true, message = "Image deleted successfully" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Failed to delete image" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while deleting the image: " + ex.Message });
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




        [HttpPost]
        public IActionResult AddComment(int postID, string content, IFormFile imageFile)
        {
            string currentUserId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
            {
                return Json(new { success = false, message = "User not logged in" });
            }

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
                string sql = "INSERT INTO forum_comments (PostID, UserID, Content, Image, CreatedAt) VALUES (@PostID, @UserID, @Content, @Image, @CreatedAt)";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@PostID", postID);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@Content", content);
                    cmd.Parameters.AddWithValue("@Image", imageData);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                    int result = cmd.ExecuteNonQuery();
                    if (result > 0)
                    {
                        int commentID = (int)cmd.LastInsertedId;
                        var comment = new
                        {
                            CommentID = commentID,
                            PostID = postID,
                            UserID = userId,
                            Content = content,
                            Image = imageData != null ? Convert.ToBase64String(imageData) : null,
                            CreatedAt = DateTime.Now,
                            Username = HttpContext.Session.GetString("Username")
                        };
                        return Json(new { success = true, comment = comment });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Failed to add comment." });
                    }
                }
            }
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

        [HttpPost]
        public IActionResult EditComment(int commentID, string content, IFormFile imageFile)
        {
            string currentUserId = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
            {
                return Json(new { success = false, message = "User not logged in" });
            }

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

                // Check if the comment belongs to the user
                string sqlCheck = "SELECT UserID FROM forum_comments WHERE CommentID = @CommentID";
                using (MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@CommentID", commentID);
                    object result = cmdCheck.ExecuteScalar();

                    if (result == null)
                    {
                        return Json(new { success = false, message = "Comment not found." });
                    }

                    int commentUserId = Convert.ToInt32(result);
                    if (commentUserId != userId)
                    {
                        return Json(new { success = false, message = "Unauthorized" });
                    }
                }

                // Update the comment
                string sql = "UPDATE forum_comments SET Content = @Content";
                if (imageData != null)
                {
                    sql += ", Image = @Image";
                }
                sql += " WHERE CommentID = @CommentID AND UserID = @UserID";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Content", content);
                    cmd.Parameters.AddWithValue("@CommentID", commentID);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    if (imageData != null)
                    {
                        cmd.Parameters.AddWithValue("@Image", imageData);
                    }

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        string imageUrl = null;
                        if (imageData != null)
                        {
                            imageUrl = "data:image/png;base64," + Convert.ToBase64String(imageData);
                        }
                        return Json(new { success = true, message = "Comment updated successfully!", imageUrl = imageUrl });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Update failed." });
                    }
                }
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
                }

                // Get updated vote counts
                string countSql = @"
            SELECT 
                (SELECT COUNT(*) FROM forum_post_votes WHERE PostID = @PostID AND VoteValue = 1) AS Upvotes,
                (SELECT COUNT(*) FROM forum_post_votes WHERE PostID = @PostID AND VoteValue = -1) AS Downvotes";
                MySqlCommand countCmd = new MySqlCommand(countSql, conn);
                countCmd.Parameters.AddWithValue("@PostID", postID);
                using (var reader = countCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int upvotes = reader.GetInt32("Upvotes");
                        int downvotes = reader.GetInt32("Downvotes");
                        return Json(new { success = true, upvotes = upvotes, downvotes = downvotes, newVoteValue = voteValue });
                    }
                }
            }
            return Json(new { success = false, message = "An error occurred while processing the vote." });
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
                    }
                    else
                    {
                        string updateSql = "UPDATE forum_comment_votes SET VoteValue=@VoteValue WHERE UserID=@UserID AND CommentID=@CommentID";
                        MySqlCommand updateCmd = new MySqlCommand(updateSql, conn);
                        updateCmd.Parameters.AddWithValue("@VoteValue", voteValue);
                        updateCmd.Parameters.AddWithValue("@UserID", userID);
                        updateCmd.Parameters.AddWithValue("@CommentID", commentID);
                        updateCmd.ExecuteNonQuery();
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
                }

                // Get updated vote counts
                string countSql = @"
            SELECT 
                (SELECT COUNT(*) FROM forum_comment_votes WHERE CommentID = @CommentID AND VoteValue = 1) AS Upvotes,
                (SELECT COUNT(*) FROM forum_comment_votes WHERE CommentID = @CommentID AND VoteValue = -1) AS Downvotes";
                MySqlCommand countCmd = new MySqlCommand(countSql, conn);
                countCmd.Parameters.AddWithValue("@CommentID", commentID);
                using (var reader = countCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int upvotes = reader.GetInt32("Upvotes");
                        int downvotes = reader.GetInt32("Downvotes");
                        return Json(new { success = true, upvotes = upvotes, downvotes = downvotes, newVoteValue = voteValue });
                    }
                }
            }
            return Json(new { success = false, message = "An error occurred while processing the vote." });
        }



    }
}
