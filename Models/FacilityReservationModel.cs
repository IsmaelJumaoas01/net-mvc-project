using System;

namespace homeowner.Models
{
    public class FacilityReservationModel
    {
        public int ReservationID { get; set; }
        public int? UserID { get; set; }
        public int FacilityID { get; set; }  // foreign key from facilities table
        public DateTime ReservationDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; }  // "Pending", "Confirmed", "Cancelled"
        // Optional: facility name for display purposes from the join.
        public string FacilityName { get; set; }
    }
}
