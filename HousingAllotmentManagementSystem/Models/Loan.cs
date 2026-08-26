using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

[Index("LoanNumber", Name = "UQ__Loans__EEC26628AAEF7968", IsUnique = true)]
public partial class Loan
{
    [Key]
    public int LoanId { get; set; }

    public int AllotmentId { get; set; }

    [StringLength(50)]
    public string LoanNumber { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal LoanAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DownPayment { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal InterestRate { get; set; }

    public int LoanTenure { get; set; }

    [Column("EMIAmount", TypeName = "decimal(18, 2)")]
    public decimal Emiamount { get; set; }

    public DateOnly SanctionDate { get; set; }

    [StringLength(30)]
    public string LoanStatus { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [ForeignKey("AllotmentId")]
    [InverseProperty("Loans")]
    public virtual Allotment Allotment { get; set; } = null!;

    [InverseProperty("Loan")]
    public virtual ICollection<Emiplan> Emiplans { get; set; } = new List<Emiplan>();
}
