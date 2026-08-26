using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

[Index("AmenityName", Name = "UQ__Amenitie__7B4A459F5B470095", IsUnique = true)]
public partial class Amenity
{
    [Key]
    public int AmenityId { get; set; }

    [StringLength(100)]
    public string AmenityName { get; set; } = null!;

    public bool Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [InverseProperty("Amenity")]
    public virtual ICollection<PropertyAmenity> PropertyAmenities { get; set; } = new List<PropertyAmenity>();
}
