using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

[Table("EMIPlans")]
public partial class Emiplan
{
    [Key]
    [Column("EMIPlanId")]
    public int EmiplanId { get; set; }

    public int LoanId { get; set; }

    [Column("EMIStartDate")]
    public DateOnly EmistartDate { get; set; }

    [Column("EMIEndDate")]
    public DateOnly EmiendDate { get; set; }

    [Column("TotalEMIs")]
    public int TotalEmis { get; set; }

    [Column("PaidEMIs")]
    public int PaidEmis { get; set; }

    [Column("RemainingEMIs")]
    public int RemainingEmis { get; set; }

    [Column("MonthlyEMI", TypeName = "decimal(18, 2)")]
    public decimal MonthlyEmi { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal OutstandingBalance { get; set; }

    public DateOnly NextDueDate { get; set; }

    [StringLength(20)]
    public string PlanStatus { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [InverseProperty("Emiplan")]
    public virtual ICollection<Installment> Installments { get; set; } = new List<Installment>();

    [ForeignKey("LoanId")]
    [InverseProperty("Emiplans")]
    public virtual Loan Loan { get; set; } = null!;
}
