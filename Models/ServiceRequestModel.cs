using System;

namespace homeowner.Models
{
    public class ServiceRequestModel
    {
        public int RequestID { get; set; }
        public int UserID { get; set; }
        public int ServiceTypeID { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } // Directly using the string since it's an ENUM in MySQL
        public DateTime RequestDate { get; set; }
        public DateTime? ScheduledDate { get; set; } // Nullable for scheduling

        public string ServiceTypeName { get; set; } // This stores the service type name

    }
}
