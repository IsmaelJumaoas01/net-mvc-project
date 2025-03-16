using System;
using System.ComponentModel.DataAnnotations;

namespace homeowner.Models
{
    public class ContactDirectoryModel
    {
        public int ContactID { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Position is required")]
        [StringLength(100, ErrorMessage = "Position cannot be longer than 100 characters")]
        public string Position { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [StringLength(100, ErrorMessage = "Department cannot be longer than 100 characters")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$", ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Office location is required")]
        [StringLength(200, ErrorMessage = "Office location cannot be longer than 200 characters")]
        public string OfficeLocation { get; set; }

        [Required(ErrorMessage = "Office hours are required")]
        [StringLength(100, ErrorMessage = "Office hours cannot be longer than 100 characters")]
        public string OfficeHours { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 