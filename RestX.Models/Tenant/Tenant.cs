using RestX.Models.Admin;
using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;

namespace RestX.Models.Tenants;

public partial class Tenant : Entity<int>
{
    public string Prefix { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? LogoUrl { get; set; }

    public string? FaviconUrl { get; set; }

    public string? BackgroundUrl { get; set; }

    public string? BaseColor { get; set; }

    public string? PrimaryColor { get; set; }

    public string? SecondaryColor { get; set; }

    public int? PlanId { get; set; }

    public string? NetworkIp { get; set; }

    public string? ConnectionString { get; set; }

    public bool? Status { get; set; }

    public string? Domain { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual Plan? Plan { get; set; }

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    public virtual ICollection<TenantSetting> TenantSettings { get; set; } = new List<TenantSetting>();
}
