using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using homeowner.Models;
using System.Collections.Generic;
using System;
using System.IO;

namespace homeowner.Controllers
{
    public class ForumController : Controller
    {
        private readonly string connectionString = "server=localhost;database=HOMEOWNERS_DB;user=root;password=;";

        // GET: /Forum/
        public IActionResult Index()
        {
            List<ForumPostModel> posts = new List<ForumPostModel>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // Retrieve all posts ordered by creation date descending.
                string sql = "SELECT * FROM forum_posts ORDER BY CreatedAt DESC";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    posts.Add(new ForumPostModel
                    {
                        PostID = reader.GetInt32("PostID"),
                        UserID = reader.IsDBNull(reader.GetOrdinal("UserID")) ? (int?)null : reader.GetInt32("UserID"),
                        Title = reader.GetString("Title"),
                        Content = reader.GetString("Content"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        Image = reader.IsDBNull(reader.GetOrdinal("Image")) ? null : (byte[])reader["Image"]
                    });
                }
                reader.Close();
            }
            return View("~/Views/Homeowner/Forum.cshtml", posts);
        }

        // GET: /Forum/Details/5[Route("forum-post/{id}")]
        public IActionResult Details(int id)
        {
            ForumPostModel post = null;
            List<ForumCommentModel> comments = new List<ForumCommentModel>();

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
                string sqlComments = "SELECT * FROM forum_comments WHERE PostID = @PostID ORDER BY CreatedAt ASC";
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
                                Image = readerComments.IsDBNull(readerComments.GetOrdinal("Image")) ? null : (byte[])readerComments["Image"]
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

        // GET: /Forum/EditPost/5
        public IActionResult EditPost(int id)
        {
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
            // Check that current user owns the post (or add admin override if desired)
            string currentUserId = HttpContext.Session.GetString("UserID");
            if (post == null || post.UserID.ToString() != currentUserId)
            {
                return RedirectToAction("Index");
            }
            return View(post);
        }

        // POST: /Forum/EditPost
        [HttpPost]
        public IActionResult EditPost(ForumPostModel model, IFormFile imageFile)
        {
            string userIdStr = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Account");
            }
            // Only allow editing if the current user owns the post.
            int userID = int.Parse(userIdStr);

            // Check ownership first.
            ForumPostModel existing = null;
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sqlCheck = "SELECT * FROM forum_posts WHERE PostID = @PostID";
                MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, conn);
                cmdCheck.Parameters.AddWithValue("@PostID", model.PostID);
                var reader = cmdCheck.ExecuteReader();
                if (reader.Read())
                {
                    existing = new ForumPostModel
                    {
                        PostID = reader.GetInt32("PostID"),
                        UserID = reader.IsDBNull(reader.GetOrdinal("UserID")) ? (int?)null : reader.GetInt32("UserID")
                    };
                }
                reader.Close();
            }
            if (existing == null || existing.UserID != userID)
            {
                return RedirectToAction("Index");
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
                string sql = "UPDATE forum_posts SET Title=@Title, Content=@Content, Image=@Image WHERE PostID=@PostID";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Title", model.Title);
                cmd.Parameters.AddWithValue("@Content", model.Content);
                cmd.Parameters.AddWithValue("@Image", imageData ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PostID", model.PostID);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        // POST: /Forum/DeletePost/5
        [HttpPost]
        public IActionResult DeletePost(int id)
        {
            string userIdStr = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return Json(new { success = false, message = "User not logged in" });
            }
            int userID = int.Parse(userIdStr);

            // Verify post ownership before deletion.
            ForumPostModel post = null;
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sqlCheck = "SELECT * FROM forum_posts WHERE PostID = @PostID";
                MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, conn);
                cmdCheck.Parameters.AddWithValue("@PostID", id);
                var reader = cmdCheck.ExecuteReader();
                if (reader.Read())
                {
                    post = new ForumPostModel
                    {
                        PostID = reader.GetInt32("PostID"),
                        UserID = reader.IsDBNull(reader.GetOrdinal("UserID")) ? (int?)null : reader.GetInt32("UserID")
                    };
                }
                reader.Close();
            }
            if (post == null || post.UserID != userID)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM forum_posts WHERE PostID = @PostID";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@PostID", id);
                cmd.ExecuteNonQuery();
            }
            return Json(new { success = true, message = "Post deleted." });
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
        public IActionResult EditComment(ForumCommentModel comment, IFormFile imageFile)
        {
            string userIdStr = HttpContext.Session.GetString("UserID");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Account");
            }
            int userID = int.Parse(userIdStr);

            // Ensure the comment belongs to the current user.
            ForumCommentModel existing = null;
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sqlCheck = "SELECT * FROM forum_comments WHERE CommentID = @CommentID";
                MySqlCommand cmdCheck = new MySqlCommand(sqlCheck, conn);
                cmdCheck.Parameters.AddWithValue("@CommentID", comment.CommentID);
                var reader = cmdCheck.ExecuteReader();
                if (reader.Read())
                {
                    existing = new ForumCommentModel
                    {
                        CommentID = reader.GetInt32("CommentID"),
                        UserID = reader.GetInt32("UserID")
                    };
                }
                reader.Close();
            }
            if (existing == null || existing.UserID != userID)
            {
                return RedirectToAction("Details", new { id = comment.PostID });
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
                string sql = "UPDATE forum_comments SET Content = @Content, Image = @Image WHERE CommentID = @CommentID";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Content", comment.Content);
                cmd.Parameters.AddWithValue("@Image", imageData ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CommentID", comment.CommentID);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Details", new { id = comment.PostID });
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
