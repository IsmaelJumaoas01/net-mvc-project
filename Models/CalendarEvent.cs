using System;

namespace homeowner.Models
{
    public class CalendarEvent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public bool AllDay { get; set; }
    }
} 