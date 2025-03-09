using System;

namespace homeowner.Models
{
    public class FeedbackComplaintModel
    {
        public int FeedbackID { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 