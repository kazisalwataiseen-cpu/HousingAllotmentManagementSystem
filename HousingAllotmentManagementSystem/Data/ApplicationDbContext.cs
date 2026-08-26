using System;
using System.Collections.Generic;
using HousingAllotmentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HousingAllotmentManagementSystem.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Allotment> Allotments { get; set; }

    public virtual DbSet<Amenity> Amenities { get; set; }

    public virtual DbSet<Application> Applications { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Emiplan> Emiplans { get; set; }

    public virtual DbSet<HousingScheme> HousingSchemes { get; set; }

    public virtual DbSet<Installment> Installments { get; set; }

    public virtual DbSet<Loan> Loans { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<PropertyAmenity> PropertyAmenities { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserDocument> UserDocuments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("AITStudent");

        modelBuilder.Entity<Allotment>(entity =>
        {
            entity.HasKey(e => e.AllotmentId).HasName("PK__Allotmen__E9FEF60FD165CD91");

            entity.Property(e => e.AllotmentDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.AllotmentStatus).HasDefaultValue("Booked");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Application).WithMany(p => p.Allotments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Allotments_Applications");

            entity.HasOne(d => d.Property).WithMany(p => p.Allotments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Allotments_Properties");
        });

        modelBuilder.Entity<Amenity>(entity =>
        {
            entity.HasKey(e => e.AmenityId).HasName("PK__Amenitie__842AF50BF8FBAB69");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue(true);
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.ApplicationId).HasName("PK__Applicat__C93A4C99CA7CB274");

            entity.Property(e => e.ApplicationDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.Property).WithMany(p => p.Applications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Applications_Properties");

            entity.HasOne(d => d.User).WithMany(p => p.Applications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Applications_Users");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("PK__AuditLog__EB5F6CBDAA30C653");

            entity.Property(e => e.ActionDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs).HasConstraintName("FK_AuditLogs_Users");
        });

        modelBuilder.Entity<Emiplan>(entity =>
        {
            entity.HasKey(e => e.EmiplanId).HasName("PK__EMIPlans__201438C967969F60");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PlanStatus).HasDefaultValue("Active");

            entity.HasOne(d => d.Loan).WithMany(p => p.Emiplans)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EMIPlans_Loans");
        });

        modelBuilder.Entity<HousingScheme>(entity =>
        {
            entity.HasKey(e => e.SchemeId).HasName("PK__HousingS__DB7E1A627551CBA3");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue(true);
        });

        modelBuilder.Entity<Installment>(entity =>
        {
            entity.HasKey(e => e.InstallmentId).HasName("PK__Installm__42B42D82FDA40456");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PaymentStatus).HasDefaultValue("Pending");

            entity.HasOne(d => d.Emiplan).WithMany(p => p.Installments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Installments_EMIPlans");
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.LoanId).HasName("PK__Loans__4F5AD457605EC373");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LoanStatus).HasDefaultValue("Active");

            entity.HasOne(d => d.Allotment).WithMany(p => p.Loans)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Loans_Allotments");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E129114B071");

            entity.Property(e => e.SentDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Sent");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_Users");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A388E5BF6BA");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PaymentDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PaymentStatus).HasDefaultValue("Success");

            entity.HasOne(d => d.Installment).WithMany(p => p.Payments).HasConstraintName("FK_Payments_Installments");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Users");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.PropertyId).HasName("PK__Properti__70C9A73536F11736");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Scheme).WithMany(p => p.Properties)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Propertie__Schem__164F3FA9");
        });

        modelBuilder.Entity<PropertyAmenity>(entity =>
        {
            entity.HasKey(e => e.PropertyAmenityId).HasName("PK__Property__0D6BB45E11D852F0");

            entity.HasOne(d => d.Amenity).WithMany(p => p.PropertyAmenities).HasConstraintName("FK__PropertyA__Ameni__20CCCE1C");

            entity.HasOne(d => d.Property).WithMany(p => p.PropertyAmenities).HasConstraintName("FK__PropertyA__Prope__1FD8A9E3");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A64327652");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C23EDA423");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsVerified).HasDefaultValue(false);
            entity.Property(e => e.Status).HasDefaultValue(true);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__0EAE1DE1");
        });

        modelBuilder.Entity<UserDocument>(entity =>
        {
            entity.HasKey(e => e.DocumentId).HasName("PK__UserDocu__1ABEEF0FAE576FBE");

            entity.Property(e => e.UploadedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.VerificationStatus).HasDefaultValue("Pending");

            entity.HasOne(d => d.User).WithMany(p => p.UserDocuments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserDocuments_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
