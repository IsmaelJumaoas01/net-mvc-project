using System;
using System.ComponentModel.DataAnnotations;

namespace homeowner.Models
{
    public class FeedbackModel
    {
        public int FeedbackID { get; set; }

        public int UserID { get; set; }

        public string Username { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
} 