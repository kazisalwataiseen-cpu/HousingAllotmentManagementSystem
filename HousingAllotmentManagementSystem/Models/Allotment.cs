using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

[Index("AllotmentNumber", Name = "UQ__Allotmen__F43E412123E3D83C", IsUnique = true)]
public partial class Allotment
{
    [Key]
    public int AllotmentId { get; set; }

    public int ApplicationId { get; set; }

    public int PropertyId { get; set; }

    [StringLength(50)]
    public string AllotmentNumber { get; set; } = null!;

    public DateOnly AllotmentDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal BookingAmount { get; set; }

    [StringLength(30)]
    public string AllotmentStatus { get; set; } = null!;

    [StringLength(500)]
    public string? Remarks { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [ForeignKey("ApplicationId")]
    [InverseProperty("Allotments")]
    public virtual Application Application { get; set; } = null!;

    [InverseProperty("Allotment")]
    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();

    [ForeignKey("PropertyId")]
    [InverseProperty("Allotments")]
    public virtual Property Property { get; set; } = null!;
}
