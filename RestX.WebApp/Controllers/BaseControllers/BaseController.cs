using AutoMapper;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;
using RestX.Models.Tenants;

namespace RestX.WebApp.Controllers.BaseControllers
    {
        public class BaseController : Controller
        {
            public readonly ActiveTenant CurrentTenant;
            public readonly IExceptionHandler ExceptionHandler;
            public readonly IMapper Mapper;
            public readonly UserManager<ApplicationUser> UserManager;

            internal Task<ApplicationUser> GetCurrentUserAsync() => this.UserManager.GetUserAsync(this.HttpContext.User);

            internal async Task<bool> IsCurrentUserAdminUser()
            {
                var rolesToCheck = UtilitiesHelper.AdminRoles.Split(",");
                var isAdmin = false;

                foreach (var role in rolesToCheck)
                {
                    isAdmin = await UserManager.IsInRoleAsync((await this.GetCurrentUserAsync()), role);

                    if (isAdmin) return true;
                }

                return isAdmin;
            }

            public override async Task OnActionExecutionAsync(
                ActionExecutingContext context,
                ActionExecutionDelegate next)
            {
                try
                {
                    if (this.CurrentTenant != null && HttpContext != null)
                    {
                        var requestTelemetry = HttpContext?.Features?.Get<RequestTelemetry>();
                        if (requestTelemetry != null)
                        {
                            requestTelemetry.Properties["TenantId"] = CurrentTenant.Id.ToString();
                            requestTelemetry.Properties["TenantName"] = CurrentTenant.Name;
                            var user = await GetCurrentUserAsync();
                            if (user != null)
                            {
                                requestTelemetry.Properties["UserName"] = user.UserName;
                                requestTelemetry.Properties["UserId"] = user.Id.ToString();
                            }
                        }
                    }
                }
                finally
                {
                    await base.OnActionExecutionAsync(context, next);
                }
            }

            public BaseController(
                IMapper mapper,
                UserManager<ApplicationUser> userManager,
                IExceptionHandler exceptionHandler,
                IEnumerable<ActiveTenant> tenant)
            {
                this.Mapper = mapper;
                this.UserManager = userManager;
                this.ExceptionHandler = exceptionHandler;
                this.CurrentTenant = tenant.FirstOrDefault();
            }

        }
    }

