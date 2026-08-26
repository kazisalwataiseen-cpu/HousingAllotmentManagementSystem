using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

[Index("TransactionId", Name = "UQ__Payments__55433A6A71835DA9", IsUnique = true)]
[Index("ReceiptNumber", Name = "UQ__Payments__C08AFDAB7B50AA5D", IsUnique = true)]
public partial class Payment
{
    [Key]
    public int PaymentId { get; set; }

    public int? InstallmentId { get; set; }

    public int UserId { get; set; }

    [StringLength(50)]
    public string PaymentType { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime PaymentDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [StringLength(50)]
    public string PaymentMethod { get; set; } = null!;

    [StringLength(100)]
    public string? TransactionId { get; set; }

    [StringLength(50)]
    public string? ReceiptNumber { get; set; }

    [StringLength(20)]
    public string PaymentStatus { get; set; } = null!;

    [StringLength(500)]
    public string? Remarks { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [ForeignKey("InstallmentId")]
    [InverseProperty("Payments")]
    public virtual Installment? Installment { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Payments")]
    public virtual User User { get; set; } = null!;
}
