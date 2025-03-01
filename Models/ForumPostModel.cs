namespace homeowner.Models
{
    public class ForumPostModel
    {
        public int PostID { get; set; }
        public int? UserID { get; set; }
        public string Username { get; set; } // ✅ New Property
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public byte[] Image { get; set; }

    public int Upvotes { get; set; }
    public int Downvotes { get; set; }

    }
}
