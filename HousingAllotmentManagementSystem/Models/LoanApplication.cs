using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousingAllotmentManagementSystem.Models
{
    [Table("LoanApplications")]
    public class LoanApplication
    {
        [Key]
        public int LoanApplicationId { get; set; }

        public int UserId { get; set; }

        public int AllotmentId { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Requested Loan Amount")]
        public decimal RequestedLoanAmount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Down Payment")]
        public decimal DownPayment { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        [Display(Name = "Interest Rate")]
        public decimal InterestRate { get; set; }

        [Display(Name = "Loan Tenure")]
        public int LoanTenure { get; set; }


        // =========================================================
        // EMI PLAN OPTION
        // =========================================================
        [Display(Name = "EMI Plan")]
        public int? EMIPlanOptionId { get; set; }
        [ForeignKey(nameof(EMIPlanOptionId))]
        public virtual EMIPlanOption? EMIPlanOption { get; set; }


        // =========================================================
        // APPLICATION DETAILS
        // =========================================================

        [Column(TypeName = "datetime")]
        public DateTime ApplicationDate { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string? Remarks { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? ReviewedDate { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedDate { get; set; }


        // =========================================================
        // NAVIGATION PROPERTIES
        // =========================================================

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("AllotmentId")]
        public virtual Allotment Allotment { get; set; } = null!;
    }
}