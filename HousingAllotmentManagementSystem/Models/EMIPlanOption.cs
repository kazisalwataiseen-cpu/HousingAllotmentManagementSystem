using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousingAllotmentManagementSystem.Models
{
    public class EMIPlanOption
    {
        [Key]
        public int EMIPlanOptionId { get; set; }

        [Required]
        [Display(Name = "Housing Scheme")]
        public int SchemeId { get; set; }

        [ForeignKey("SchemeId")]
        public virtual HousingScheme? HousingScheme { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Plan Name")]
        public string PlanName { get; set; } = string.Empty;

        [Required]
        [Range(1, 600)]
        [Display(Name = "Tenure (Months)")]
        public int TenureMonths { get; set; }

        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(5, 2)")]
        [Display(Name = "Interest Rate (%)")]
        public decimal InterestRate { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [Column(TypeName = "datetime")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}