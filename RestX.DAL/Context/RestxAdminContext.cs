using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RestX.Models.Tenants;

namespace RestX.Models.Admin;

public partial class RestxAdminContext : DbContext
{
    public RestxAdminContext()
    {
    }

    public RestxAdminContext(DbContextOptions<RestxAdminContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Plan> Plans { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<TenantSetting> TenantSettings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=TIS;Initial Catalog=RESTX_ADMIN;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__admins__3213E83F38D735A7");

            entity.ToTable("admins");

            entity.HasIndex(e => e.Email, "UQ__admins__AB6E6164CB40E1C8").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__audit_lo__3213E83F8E7D962C");

            entity.ToTable("audit_logs");

            entity.HasIndex(e => e.TenantId, "IX_AuditLogs_Tenant");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .HasColumnName("action");
            entity.Property(e => e.ActorAdminId).HasColumnName("actor_admin_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Metadata).HasColumnName("metadata");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");

            entity.HasOne(d => d.Tenant).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.TenantId)
                .HasConstraintName("FK_Audit_Tenant");
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__plans__3213E83F0A554473");

            entity.ToTable("plans");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.MaxStorageMb).HasColumnName("max_storage_mb");
            entity.Property(e => e.MaxUsers).HasColumnName("max_users");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PriceMonthly)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price_monthly");
            entity.Property(e => e.PriceYearly)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price_yearly");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__subscrip__3213E83FA1385A06");

            entity.ToTable("subscriptions");

            entity.HasIndex(e => e.TenantId, "IX_Subscriptions_Tenant");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BillingCycle)
                .HasMaxLength(50)
                .HasColumnName("billing_cycle");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.RenewsAt).HasColumnName("renews_at");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");

            entity.HasOne(d => d.Plan).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sub_Plan");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sub_Tenant");
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tenants__3213E83F062A1797");

            entity.ToTable("tenants");

            entity.HasIndex(e => e.Prefix, "IX_Tenants_Prefix");

            entity.HasIndex(e => e.Prefix, "UQ__tenants__DA929218A2137A2B").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BackgroundUrl)
                .HasMaxLength(255)
                .HasColumnName("background_url");
            entity.Property(e => e.BaseColor)
                .HasMaxLength(50)
                .HasColumnName("base_color");
            entity.Property(e => e.ConnectionString).HasColumnName("connection_string");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(255)
                .HasColumnName("created_by");
            entity.Property(e => e.Domain)
                .HasMaxLength(255)
                .HasColumnName("domain");
            entity.Property(e => e.ExpiredAt).HasColumnName("expired_at");
            entity.Property(e => e.FaviconUrl)
                .HasMaxLength(255)
                .HasColumnName("favicon_url");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(255)
                .HasColumnName("logo_url");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.NetworkIp)
                .HasMaxLength(100)
                .HasColumnName("network_ip");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.Prefix)
                .HasMaxLength(100)
                .HasColumnName("prefix");
            entity.Property(e => e.PrimaryColor)
                .HasMaxLength(50)
                .HasColumnName("primary_color");
            entity.Property(e => e.SecondaryColor)
                .HasMaxLength(50)
                .HasColumnName("secondary_color");
            entity.Property(e => e.Status)
                .HasDefaultValue(true)
                .HasColumnName("status");

            entity.HasOne(d => d.Plan).WithMany(p => p.Tenants)
                .HasForeignKey(d => d.PlanId)
                .HasConstraintName("FK_Tenants_Plans");
        });

        modelBuilder.Entity<TenantSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tenant_s__3213E83FFBDA1AA3");

            entity.ToTable("tenant_settings");

            entity.HasIndex(e => new { e.TenantId, e.Key }, "UQ_Tenant_Key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Key)
                .HasMaxLength(100)
                .HasColumnName("key");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(d => d.Tenant).WithMany(p => p.TenantSettings)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Settings_Tenants");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
