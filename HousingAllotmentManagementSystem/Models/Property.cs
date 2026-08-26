using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

public partial class Property
{
    [Key]
    public int PropertyId { get; set; }

    public int SchemeId { get; set; }

    [StringLength(30)]
    public string? UnitNumber { get; set; }

    [StringLength(30)]
    public string? PlotNumber { get; set; }

    [StringLength(50)]
    public string? PropertyType { get; set; }

    public int? Bedrooms { get; set; }

    public int? Bathrooms { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? CarpetArea { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? BuiltupArea { get; set; }

    [StringLength(300)]
    public string? FloorPlanImage { get; set; }

    [StringLength(30)]
    public string? Facing { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Price { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? BookingAmount { get; set; }

    [StringLength(30)]
    public string? Status { get; set; }

    public string? Description { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [InverseProperty("Property")]
    public virtual ICollection<Allotment> Allotments { get; set; } = new List<Allotment>();

    [InverseProperty("Property")]
    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    [InverseProperty("Property")]
    public virtual ICollection<PropertyAmenity> PropertyAmenities { get; set; } = new List<PropertyAmenity>();

    [ForeignKey("SchemeId")]
    [InverseProperty("Properties")]
    public virtual HousingScheme Scheme { get; set; } = null!;
}
