using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

public partial class HousingScheme
{
    [Key]
    public int SchemeId { get; set; }

    [StringLength(200)]
    public string SchemeName { get; set; } = null!;

    public string? Description { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(250)]
    public string? Location { get; set; }

    public DateOnly? LaunchDate { get; set; }

    public DateOnly? LastApplicationDate { get; set; }

    public int? TotalUnits { get; set; }

    [StringLength(300)]
    public string? Brochure { get; set; }

    [StringLength(300)]
    public string? BannerImage { get; set; }

    public bool? Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [InverseProperty("Scheme")]
    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
