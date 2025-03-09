using System.ComponentModel.DataAnnotations;

namespace homeowner.Models
{
    public class ForumPostModel
    {
        public int PostID { get; set; }
        public int? UserID { get; set; }
        
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        public byte[]? Image { get; set; }
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
        public bool HasUpvoted { get; set; }
        public bool HasDownvoted { get; set; }
    }
}
