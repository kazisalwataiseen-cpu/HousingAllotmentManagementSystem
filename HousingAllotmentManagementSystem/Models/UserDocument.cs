using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

public partial class UserDocument
{
    [Key]
    public int DocumentId { get; set; }

    public int UserId { get; set; }

    [StringLength(255)]
    public string? AadhaarCard { get; set; }

    [Column("PANCard")]
    [StringLength(255)]
    public string? Pancard { get; set; }

    [StringLength(255)]
    public string? IncomeCertificate { get; set; }

    [StringLength(255)]
    public string? SalarySlip { get; set; }

    [StringLength(255)]
    public string? PassportPhoto { get; set; }

    [StringLength(255)]
    public string? BankStatement { get; set; }

    [StringLength(255)]
    public string? OtherDocument { get; set; }

    [StringLength(20)]
    public string VerificationStatus { get; set; } = null!;

    [StringLength(500)]
    public string? Remarks { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UploadedDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? VerifiedDate { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserDocuments")]
    public virtual User User { get; set; } = null!;
}
