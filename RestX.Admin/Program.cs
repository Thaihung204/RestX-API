using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestX.AdminDAL.Context;
using RestX.BLL;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.BLL.Services;
using RestX.DAL.Context;
using RestX.Models.Admin;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add DB Context 
builder.Services.AddDbContext<RestxAdminContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionString")));

//Add HangFire 
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(
              builder.Configuration.GetConnectionString("HangfireConnection"),
              new SqlServerStorageOptions
              {
                  CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                  SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                  QueuePollInterval = TimeSpan.FromSeconds(15),
                  UseRecommendedIsolationLevel = true,
                  DisableGlobalLocks = true
              });
});
builder.Services.AddHangfireServer();

//Add Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("MultiTenantCors", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return uri.Host.EndsWith(".restx.food");
                }
                return false;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IRepository, EntityFrameworkRepository<RestxAdminContext>>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IExceptionHandler, ExceptionHandler>();
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddIdentity<Admin, IdentityRole>().AddEntityFrameworkStores<RestxAdminContext>().AddDefaultTokenProviders();
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));

builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
var app = builder.Build();
app.UseCors("MultiTenantCors");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<RestxAdminContext>();
        await context.Database.MigrateAsync();
        Console.WriteLine("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while migrating the database: {ex.Message}");
        throw;
    }
}

app.UseHangfireDashboard("/hangfire");
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    //app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
