using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

public partial class Installment
{
    [Key]
    public int InstallmentId { get; set; }

    [Column("EMIPlanId")]
    public int EmiplanId { get; set; }

    public int InstallmentNumber { get; set; }

    public DateOnly DueDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal InstallmentAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PrincipalAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal InterestAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal LateFee { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PaidAmount { get; set; }

    public DateOnly? PaymentDate { get; set; }

    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    [StringLength(100)]
    public string? TransactionReference { get; set; }

    [StringLength(20)]
    public string PaymentStatus { get; set; } = null!;

    [StringLength(500)]
    public string? Remarks { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [ForeignKey("EmiplanId")]
    [InverseProperty("Installments")]
    public virtual Emiplan Emiplan { get; set; } = null!;

    [InverseProperty("Installment")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
