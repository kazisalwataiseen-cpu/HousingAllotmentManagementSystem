using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

[Index("Mobile", Name = "UQ__Users__6FAE0782435CDC2F", IsUnique = true)]
[Index("Email", Name = "UQ__Users__A9D10534DE3FB8E8", IsUnique = true)]
public partial class User
{
    [Key]
    public int UserId { get; set; }

    public int RoleId { get; set; }

    [StringLength(150)]
    public string FullName { get; set; } = null!;

    [StringLength(150)]
    public string Email { get; set; } = null!;

    [StringLength(15)]
    public string Mobile { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    [StringLength(20)]
    public string? Gender { get; set; }

    [Column("DOB")]
    public DateOnly? Dob { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(10)]
    public string? Pincode { get; set; }

    [StringLength(20)]
    public string? AadhaarNumber { get; set; }

    [Column("PANNumber")]
    [StringLength(20)]
    public string? Pannumber { get; set; }

    [StringLength(100)]
    public string? Occupation { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? AnnualIncome { get; set; }

    [StringLength(300)]
    public string? ProfileImage { get; set; }

    public bool? IsVerified { get; set; }

    public bool? Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    [InverseProperty("User")]
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role Role { get; set; } = null!;

    [InverseProperty("User")]
    public virtual ICollection<UserDocument> UserDocuments { get; set; } = new List<UserDocument>();
}
