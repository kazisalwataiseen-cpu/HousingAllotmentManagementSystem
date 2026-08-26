using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Models;

public partial class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    public int? UserId { get; set; }

    [StringLength(100)]
    public string Action { get; set; } = null!;

    [StringLength(100)]
    public string? TableName { get; set; }

    public int? RecordId { get; set; }

    public string? Description { get; set; }

    [Column("IPAddress")]
    [StringLength(50)]
    public string? Ipaddress { get; set; }

    [StringLength(255)]
    public string? BrowserInfo { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ActionDate { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("AuditLogs")]
    public virtual User? User { get; set; }
}
