using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestX.Models.Identity
{
    using Microsoft.AspNetCore.Identity;
    using RestX.Models.HR;

    public class ApplicationUser : IdentityUser
    {
        public Guid MemberId { get; set; }
        [ForeignKey("MemberId")]
        public virtual Employee Member { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public bool PushNotificationEnabled { get; set; } = true;
    }
}
