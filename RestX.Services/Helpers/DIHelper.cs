
namespace RestX.BLL.Helpers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using RestX.BLL.Interfaces;
    using RestX.BLL.Interfaces.Auth;
    using RestX.BLL.Interfaces.Customers;
    using RestX.BLL.Interfaces.Employees;
    using RestX.BLL.Services;

    public static class DIHelper
    {
        public static void Setup(IServiceCollection services, bool isDevelopment = false)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();

        }
    }
}
