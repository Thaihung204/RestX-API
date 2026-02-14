using AspNetCoreRateLimit;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using RestX.AdminDAL.Context;
using RestX.App.Helpers;
using RestX.BLL;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using RestX.BLL.Interfaces.Customers;
using RestX.BLL.Interfaces.Employees;
using RestX.BLL.MultiTenancy;
using RestX.BLL.Services;
using RestX.DAL.Context;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using RestX.WebApp.Extensions;
using SaasKit.Multitenancy;
using SaasKit.Multitenancy.Internal;
using System.Security.Claims;
using System.Text;

namespace RestX.WebApp
{
    public class Startup
    {
        private bool isDevlopement = false;
        private readonly string CurrentEnvironment = string.Empty;
        public IConfigurationRoot Configuration { get; }

        public Startup(IWebHostEnvironment env)
        {
            Environment.CurrentDirectory = env.ContentRootPath;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            isDevlopement = env.IsDevelopment();
            CurrentEnvironment = env.EnvironmentName;
            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Multi Tenant Support
            services.AddControllers();
            services.AddSingleton<RestXCookieManager>();
            services.AddScoped<IRedisService, RedisService>();

            services.AddMultitenancy<ActiveTenant, TenantResolver>();
            //services.Configure<RazorViewEngineOptions>(
            //    options => { options.ViewLocationExpanders.Add(new TenantViewLocationExpander()); });

            // Add framework services.
            services.AddDbContext<RestxAdminContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString("AdminDbContext"), options => options.EnableRetryOnFailure()));

            services.AddScoped<TenantDbContext>(serviceProvider =>
            {
                var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
                var tenantContext = httpContextAccessor?.HttpContext?.GetTenantContext<ActiveTenant>();
                var tenant = tenantContext?.Tenant;
                return new TenantDbContext(tenant);
            });

            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.AllowedUserNameCharacters = null;
            })
                .AddEntityFrameworkStores<TenantDbContext>()
                .AddDefaultTokenProviders();


            // Needed if using the force logout function on /logout
            //services.Configure<SecurityStampValidatorOptions>(options =>
            //{
            //    // enables immediate logout, after updating the user's stat.
            //    options.ValidationInterval = TimeSpan.Zero;
            //});

            services.AddCors();
            //services.AddSingleton<ICorsPolicyProvider, CustomCorsPolicyProvider>();

            // JWT Authentication
            var secret = Configuration.GetSection("AppSettings")["Secret"];
            var jwtSettings = Configuration.GetSection("JwtSettings");
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer("Bearer", cfg =>
             {
                 cfg.RequireHttpsMetadata = !isDevlopement;
                 cfg.SaveToken = true;
                 cfg.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuer = !string.IsNullOrEmpty(jwtSettings["Issuer"]),
                     ValidateAudience = !string.IsNullOrEmpty(jwtSettings["Audience"]),
                     ValidIssuer = jwtSettings["Issuer"],
                     ValidAudience = jwtSettings["Audience"],
                     ValidateIssuerSigningKey = true,
                     IssuerSigningKey = new SymmetricSecurityKey(
                         Encoding.ASCII.GetBytes(secret)
                     ),
                     ValidateLifetime = true,
                     ClockSkew = TimeSpan.Zero,
                     RoleClaimType = ClaimTypes.Role
                 };
                 cfg.Events = new JwtBearerEvents
                 {
                     OnTokenValidated = context =>
                     {
                         var tenantHostnameClaim = context.Principal?.FindFirst("tenant_hostname")?.Value;
                         if (!string.IsNullOrEmpty(tenantHostnameClaim))
                         {
                             var requestHost = context.HttpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault()
                                 ?? context.HttpContext.Request.Host.Value;
                             if (!string.Equals(tenantHostnameClaim, requestHost, StringComparison.OrdinalIgnoreCase))
                             {
                                 context.Fail("Token does not belong to this tenant");
                             }
                         }
                         return Task.CompletedTask;
                     }
                 };
             });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder(
                    JwtBearerDefaults.AuthenticationScheme
                )
                .RequireAuthenticatedUser()
                .Build();
            });


            // MSSQL Hangfire
            services.AddHangfire(x => x.UseSqlServerStorage(Configuration.GetConnectionString("AdminDbContext"),
                new SqlServerStorageOptions
                {
                    QueuePollInterval = TimeSpan.Zero
                }));
           
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHttpContextAccessor();

            services.AddApplicationInsightsTelemetry();

            services.AddSnapshotCollector();

            //services.AddWebOptimizer(BundleHelper.RegisterBundles);
            services.AddAutoMapper(typeof(AutoMapperProfile));

            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            //services.Configure<AzureAdOptions>(Configuration.GetSection("AzureAd"));
            services.Configure<ConnectionStrings>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));
            services.Configure<JwtSettings>(Configuration.GetSection("JwtSettings"));
            services.AddResponseCompression();

            SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler
            {
                // Customize this value based on desired DNS refresh timer
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };
            // Registering the Singleton SocketsHttpHandler lets you reuse it across any HttpClient in your application
            services.AddSingleton<SocketsHttpHandler>(socketsHttpHandler);
            services.AddSignalR();
            services.AddScoped<IExceptionHandler, ExceptionHandler>();
            services.AddScoped<ITenantService, TenantService>();
            services.AddSingleton<IMemoryCache, MemoryCache>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddTransient<TelemetryExtender>();

            // Feature Management
            //services.AddSingleton<IFeatureDefinitionProvider, TenantBrandFeatureProvider>()
            //    .AddFeatureManagement().UseDisabledFeaturesHandler(new DisabledFeaturesHandler());

            isDevlopement = isDevlopement || (Configuration.GetSection("AppSettings")["EmailProvider"] ?? "") == "Mailtrap";
            DIHelper.Setup(services, isDevlopement);

            services.AddCors(options =>
            {
                options.AddPolicy("CustomCorsPolicy", builder =>
                {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                });
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "RestX WebApp API",
                    Version = "v1"
                });

                c.CustomSchemaIds(type => type.FullName);
            });

            //services.AddSwaggerGenNewtonsoftSupport();

            // Setup plugins for templator
            //TemplatorHelper.RegisterPlugins();
            //if (CurrentEnvironment != "Testing" && CurrentEnvironment != "TestingRelease")
            //{
            //    var defaultApp = FirebaseApp.Create(new AppOptions()
            //    {
            //        Credential = GoogleCredential.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firebase-config.json")),
            //    });
            //}

            // Log the request headers
            services.AddHttpLogging(logging =>
            {
                logging.LoggingFields = HttpLoggingFields.RequestHeaders;
            });

            //services.AddNodeJS();

            ThreadPool.SetMinThreads(int.Parse(Environment.GetEnvironmentVariable("MinimumWorkerThreads") ?? "250"), int.Parse(Environment.GetEnvironmentVariable("MinimumIoThreads") ?? "250"));

            var documentIntelligenceEndpoint = Configuration.GetSection("AppSettings")["DocumentIntelligenceEndpoint"];
            var documentIntelligenceApiKey = Configuration.GetSection("AppSettings")["DocumentIntelligenceApiKey"];

            // Add MVC Controllers
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, TelemetryConfiguration telemetryConfiguration)
        {
            if (!env.IsProduction())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/home/error");
            }

            app.UseCors("CustomCorsPolicy");

            //if (CurrentEnvironment != "Testing" && CurrentEnvironment != "TestingRelease")
            //{
            //    app.UseSerilogRequestLogging();
            //    app.UseReact(config => { });
            //}
            //app.UseWebOptimizer();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    // Page Speed recommends to cache for a year or more
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                        "public,max-age=31536000";
                }
            });


            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "swagger";
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "RestX WebApp API v1");
            });

            app.UseMultitenancy<ActiveTenant>();

            if (env.IsProduction())
            {
                app.UseMiddleware<TenantUnresolvedRedirectMiddleware<ActiveTenant>>("https://restx.food", false);
            }
            //app.UseMiddleware<TenantRedirectMiddleware<ActiveTenant>>();
            //app.UseIpRateLimiting();
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None,
            });
            app.Use((context, next) =>
            {
                context.Response.Headers.Add("X-Endpoint",
                    Environment.GetEnvironmentVariable("APPSETTING_AppServiceId") ?? "Not Available");
                return next.Invoke();
            });
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders =
                ForwardedHeaders.XForwardedHost |
                ForwardedHeaders.XForwardedProto
            });
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<TelemetryExtender>();
            //app.ApplyMigrations();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute("api", "api/{controller}/{action}/{id?}");
                //endpoints.MapFallbackToController("Index", "Public");
            });

        }

    }
}
