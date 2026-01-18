using System;
using System.Collections.Generic;
using System.Text;

namespace RestX.BLL.MultiTenancy
{
    using RestX.Models.Tenants;

    public class MultiTenancyOptions
    {
        public List<ActiveTenant> Tenants { get; set; }
    }
}
