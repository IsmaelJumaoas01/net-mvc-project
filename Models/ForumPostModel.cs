using System;

namespace homeowner.Models
{
    public class ForumPostModel
    {
        public int PostID { get; set; }
        public int? UserID { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        // Store image as binary data; can be null.
        public byte[] Image { get; set; }
    }
}
