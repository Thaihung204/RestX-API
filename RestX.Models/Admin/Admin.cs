using RestX.Models.BaseModel;
using System;
using System.Collections.Generic;

namespace RestX.Models.Admin;

public partial class Admin : Entity<Guid>
{
    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Role { get; set; }

    public string? Status { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
