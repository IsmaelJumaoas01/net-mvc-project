using System;
using System.ComponentModel.DataAnnotations;

namespace homeowner.Models
{
    public class VehicleRegistration
    {
        public int VehicleId { get; set; }

        public int UserID { get; set; }

        public string Username { get; set; }

        [Required(ErrorMessage = "Plate number is required")]
        [StringLength(20, ErrorMessage = "Plate number cannot be longer than 20 characters")]
        public string PlateNumber { get; set; }

        [Required(ErrorMessage = "Vehicle model is required")]
        [StringLength(50, ErrorMessage = "Vehicle model cannot be longer than 50 characters")]
        public string VehicleModel { get; set; }

        [Required(ErrorMessage = "Vehicle color is required")]
        [StringLength(20, ErrorMessage = "Vehicle color cannot be longer than 20 characters")]
        public string VehicleColor { get; set; }

        [Required(ErrorMessage = "Vehicle type is required")]
        [StringLength(20, ErrorMessage = "Vehicle type cannot be longer than 20 characters")]
        public string VehicleType { get; set; }

        [Required(ErrorMessage = "Vehicle brand is required")]
        [StringLength(50, ErrorMessage = "Vehicle brand cannot be longer than 50 characters")]
        public string VehicleBrand { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Expiry date is required")]
        public DateTime ExpiryDate { get; set; }

        public string Status { get; set; } = "Active";

        public string State { get; set; } = "Pending";

        public string? Notes { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public int? ApprovedBy { get; set; }

        public string? RejectionReason { get; set; }
    }
} 