using System;
using System.ComponentModel.DataAnnotations;

namespace homeowner.Models
{
    public class VisitorPass
    {
        public int VisitorPassId { get; set; }

        public int UserID { get; set; }

        [Required(ErrorMessage = "Visitor name is required")]
        [Display(Name = "Visitor Name")]
        [StringLength(100, ErrorMessage = "Visitor name cannot be longer than 100 characters")]
        public string VisitorName { get; set; }

        [Required(ErrorMessage = "Visitor contact is required")]
        [Display(Name = "Contact Number")]
        [StringLength(20, ErrorMessage = "Contact number cannot be longer than 20 characters")]
        [RegularExpression(@"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$", ErrorMessage = "Invalid phone number format")]
        public string VisitorContact { get; set; }

        [Required(ErrorMessage = "Purpose is required")]
        [Display(Name = "Purpose of Visit")]
        [StringLength(50, ErrorMessage = "Purpose cannot be longer than 50 characters")]
        public string Purpose { get; set; }

        [Required(ErrorMessage = "Visit date is required")]
        [Display(Name = "Visit Date")]
        [DataType(DataType.Date)]
        public DateTime VisitDate { get; set; }

        [Required(ErrorMessage = "Visit time is required")]
        [Display(Name = "Visit Time")]
        [DataType(DataType.Time)]
        public TimeSpan VisitTime { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }

        public string Status { get; set; } = "Pending";

        [Display(Name = "Vehicle Plate Number")]
        [StringLength(20, ErrorMessage = "Plate number cannot be longer than 20 characters")]
        public string? VehiclePlateNumber { get; set; }

        [Display(Name = "Vehicle Model")]
        [StringLength(50, ErrorMessage = "Vehicle model cannot be longer than 50 characters")]
        public string? VehicleModel { get; set; }

        [Display(Name = "Vehicle Color")]
        [StringLength(20, ErrorMessage = "Vehicle color cannot be longer than 20 characters")]
        public string? VehicleColor { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public string? RejectionReason { get; set; }
        public string? Username { get; set; }
    }
} 