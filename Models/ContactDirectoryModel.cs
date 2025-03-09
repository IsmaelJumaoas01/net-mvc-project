using System;

namespace homeowner.Models
{
    public class ContactDirectoryModel
    {
        public int ContactID { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string OfficeLocation { get; set; }
        public string OfficeHours { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 