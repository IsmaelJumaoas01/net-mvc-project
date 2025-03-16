using System;
using System.ComponentModel.DataAnnotations;

namespace homeowner.Models
{
    public class BillType
    {
        public int BillTypeID { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }
        public bool IsRecurring { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Bill
    {
        public int BillID { get; set; }
        public int UserID { get; set; }
        public int BillTypeID { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required]
        public string Status { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public BillType BillType { get; set; }
        public UserModel User { get; set; }
    }

    public class Payment
    {
        public int PaymentID { get; set; }
        public int BillID { get; set; }
        public int UserID { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        public string ReferenceNumber { get; set; }
        public bool HasProofOfPayment { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public int? VerifiedBy { get; set; }

        // Navigation properties
        public Bill? Bill { get; set; }
        public UserModel? User { get; set; }
        public UserModel? Verifier { get; set; }
    }

    public class PaymentHistory
    {
        public int HistoryID { get; set; }
        public int PaymentID { get; set; }
        public string Action { get; set; }
        public string PreviousStatus { get; set; }
        public string NewStatus { get; set; }
        public string Notes { get; set; }
        public int PerformedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Payment Payment { get; set; }
        public UserModel Performer { get; set; }
    }

    // View Models
    public class BillingDashboardViewModel
    {
        public decimal TotalDue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOverdue { get; set; }
        public int PendingBillsCount { get; set; }
        public int OverdueBillsCount { get; set; }
        public List<Bill> RecentBills { get; set; }
        public List<Payment> RecentPayments { get; set; }
    }

    public class BillPaymentViewModel
    {
        public int BillID { get; set; }
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        public string PaymentMethod { get; set; }

        [Required(ErrorMessage = "Reference number is required")]
        public string ReferenceNumber { get; set; }

        [Required(ErrorMessage = "Payment date is required")]
        public DateTime PaymentDate { get; set; }

        public string Notes { get; set; }

        [DataType(DataType.Upload)]
        public IFormFile ProofOfPayment { get; set; }
    }

    public class BulkBillGenerationViewModel
    {
        [Required]
        public int BillTypeID { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        public string Description { get; set; }
        public List<int> SelectedUsers { get; set; }
    }
} 