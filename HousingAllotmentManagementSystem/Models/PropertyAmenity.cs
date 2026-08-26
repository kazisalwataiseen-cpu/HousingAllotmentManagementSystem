using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

public partial class PropertyAmenity
{
    [Key]
    public int PropertyAmenityId { get; set; }

    public int? PropertyId { get; set; }

    public int? AmenityId { get; set; }

    [ForeignKey("AmenityId")]
    [InverseProperty("PropertyAmenities")]
    public virtual Amenity? Amenity { get; set; }

    [ForeignKey("PropertyId")]
    [InverseProperty("PropertyAmenities")]
    public virtual Property? Property { get; set; }
}
