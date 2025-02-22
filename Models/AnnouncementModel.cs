using System;

namespace homeowner.Models
{
    public class AnnouncementModel
    {
        public int AnnouncementID { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserID { get; set; }
    }
}