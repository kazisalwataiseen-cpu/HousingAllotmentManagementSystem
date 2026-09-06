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
        public decimal RequestedLoanAmount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal DownPayment { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal InterestRate { get; set; }

        public int LoanTenure { get; set; }

        // =========================================================
        // SELECTED EMI PLAN OPTION
        // This is used only by the loan application form.
        // It will NOT create a column in the database.
        // =========================================================

        [NotMapped]
        [Display(Name = "EMI Plan")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select an EMI plan.")]
        public int EMIPlanOptionId { get; set; }

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

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("AllotmentId")]
        public virtual Allotment Allotment { get; set; } = null!;
    }
}