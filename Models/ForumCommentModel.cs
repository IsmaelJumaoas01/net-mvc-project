using System;

namespace homeowner.Models
{
    public class ForumCommentModel
    {
        public int CommentID { get; set; }
        public int PostID { get; set; }
        public int UserID { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        // Optional image stored as BLOB.
        public byte[] Image { get; set; }
    }
}
