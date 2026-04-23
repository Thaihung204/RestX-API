using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestX.Models.Admin;
using RestX.Models.Tenants;

namespace RestX.AdminDAL.Context;

public partial class RestxAdminContext : IdentityDbContext<Admin>
{
    public RestxAdminContext()
    {
    }

    public RestxAdminContext(DbContextOptions<RestxAdminContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<TenantSetting> TenantSettings { get; set; }

    public virtual DbSet<TenantRequest> TenantRequests { get; set; }

    public virtual DbSet<DepositConfig> DepositConfigs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=restx-sqlserver,1433;Database=restx_admin;User Id=sa;Password=Passw0r1!;Encrypt=False;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>();

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(x => x.Hostname)
                  .IsUnique();
            entity.Property(e => e.TaxRate).HasColumnType("decimal(5,2)");
            entity.Property(e => e.ServiceChargeRate).HasColumnType("decimal(5,2)");
        });

        modelBuilder.Entity<TenantSetting>();

        modelBuilder.Entity<DepositConfig>(entity =>
        {
            entity.HasKey(e => e.TenantId);
            entity.Property(e => e.DepositAmountPerPerson).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<TenantRequest>(entity =>
        {
            entity.Property(x => x.tenantRequestStatus)
                  .HasConversion<int>();
        });

        base.OnModelCreating(modelBuilder);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
