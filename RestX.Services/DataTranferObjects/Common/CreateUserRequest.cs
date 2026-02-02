using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.DataTranferObjects.Common
{
    public class CreateUserRequest
    {
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public required string Role { get; set; }
        public Guid? MemberId { get; set; }
        public bool GenerateRandomPassword { get; set; }
    }
}
