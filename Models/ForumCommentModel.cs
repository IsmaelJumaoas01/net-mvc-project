using System;
using System.ComponentModel.DataAnnotations;

namespace homeowner.Models
{
    public class ForumCommentModel
    {
        public int CommentID { get; set; }
        public int PostID { get; set; }
        public int UserID { get; set; }
        
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        // Optional image stored as BLOB.
        public byte[]? Image { get; set; }

        public int Upvotes {get; set;}

        public int Downvotes {get; set;}

        public string? ImagePath { get; set; }

        public bool HasUpvoted { get; set; }

        public bool HasDownvoted { get; set; }
    }
}
