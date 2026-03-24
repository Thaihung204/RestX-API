using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestX.Models.Identity
{
    using Microsoft.AspNetCore.Identity;
    using RestX.Models.HR;

    public class ApplicationUser : IdentityUser<Guid>
    {
        public Guid? MemberId { get; set; }
        [ForeignKey("MemberId")]
        public virtual Employee? Member { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public DateTime LastModified { get; set; } = DateTime.UtcNow.AddHours(7);
        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public bool PushNotificationEnabled { get; set; } = true;
        [MaxLength(500)]
        public string? AvatarUrl { get; set; }
        [MaxLength(256)]
        public string FullName { get; set; } = string.Empty;
    }
}
