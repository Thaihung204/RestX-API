namespace RestX.BLL.Helpers
{
    using CloudinaryDotNet;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using RestX.BLL.Interfaces;
    using RestX.BLL.Interfaces.Auth;
    using RestX.BLL.Interfaces.Customers;
    using RestX.BLL.Interfaces.Employees;
    using RestX.BLL.Interfaces.Status;
    using RestX.BLL.Interfaces.Inventory;
    using RestX.BLL.Interfaces.Tables;
    using RestX.BLL.Services;
    using RestX.BLL.Services.Auth;
    using RestX.DAL.Context;

    public static class DIHelper
    {
        public static void Setup(IServiceCollection services, bool isDevelopment = false)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IUserAccountService, UserAccountService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthLinkService, AuthLinkService>();       

            services.AddScoped<ICloudinaryService, CloudinaryService>();
            services.AddScoped<IRepository, EntityFrameworkRepository<TenantDbContext>>();
            services.AddScoped<IExceptionHandler, ExceptionHandler>();
            services.AddScoped<IDishService, DishService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ITableService, TableService>();
            services.AddScoped<IIngredientService, IngredientService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<IStatusValueService, StatusValueService>();
        }
    }
}
