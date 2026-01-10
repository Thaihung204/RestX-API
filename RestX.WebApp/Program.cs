using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestX.BLL.MultiTenancy;
using RestX.Models.Tenants;
using RestX.WebApp;
using Serilog;
using Serilog.Events;

namespace RestX.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var SerilogEventLevel = (Environment.GetEnvironmentVariable("SerilogEventLevel") ?? "Information") switch
            {
                "Verbose" => LogEventLevel.Verbose,
                "Debug" => LogEventLevel.Debug,
                "Information" => LogEventLevel.Information,
                "Warning" => LogEventLevel.Warning,
                "Error" => LogEventLevel.Error,
                "Fatal" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };

            if (environment == Environments.Development)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", SerilogEventLevel)
                    .Enrich.FromLogContext()
                    .CreateLogger();
            }
            else
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")))
                {
                    var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
                    telemetryConfiguration.InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
                    Log.Logger = new LoggerConfiguration()
                        .Filter.ByExcluding(e => e.Exception?.Message == "The client has disconnected")
                        .Filter.ByExcluding(e => e.Exception is DbUpdateConcurrencyException)
                        .Filter.ByExcluding(e => e.Exception is SecurityTokenExpiredException)
                        .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces, SerilogEventLevel)
                        .CreateLogger();
                }
            }

            try
            {
                Log.Information("Starting web host");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
//var builder = WebApplication.CreateBuilder(args);


//// Multi Tenant Support
//builder.Services.AddMultitenancy<ActiveTenant, TenantResolver>();

//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();
