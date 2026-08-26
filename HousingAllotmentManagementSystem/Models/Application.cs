using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

public partial class Application
{
    [Key]
    public int ApplicationId { get; set; }

    public int UserId { get; set; }

    public int PropertyId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ApplicationDate { get; set; }

    [StringLength(100)]
    public string? EmploymentType { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? AnnualIncome { get; set; }

    [StringLength(100)]
    public string? NomineeName { get; set; }

    [StringLength(50)]
    public string? NomineeRelation { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [InverseProperty("Application")]
    public virtual ICollection<Allotment> Allotments { get; set; } = new List<Allotment>();

    [ForeignKey("PropertyId")]
    [InverseProperty("Applications")]
    public virtual Property Property { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Applications")]
    public virtual User User { get; set; } = null!;
}
