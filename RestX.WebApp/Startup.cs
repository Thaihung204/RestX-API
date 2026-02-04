using AspNetCoreRateLimit;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.ApplicationInsights.Extensibility;
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

            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
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

            // Cookie Auth
            var secret = Configuration.GetSection("AppSettings")["Secret"];
            services.AddAuthentication()
                .AddCookie("Cookies")
                .AddJwtBearer("Bearer", cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
                        ClockSkew = TimeSpan.Zero,
                        ValidateLifetime = true,
                        RoleClaimType = ClaimTypes.Role //"role"
                    };
                });

            // Configure here rather than inline as settings get lost calling JWT as well.
            services.ConfigureApplicationCookie(cfg =>
            {
                cfg.CookieManager = new RestXCookieManager();
                cfg.SlidingExpiration = true;
                cfg.LoginPath = "/login";
                cfg.LogoutPath = "/logout";
                cfg.AccessDeniedPath = "/access-denied";
                cfg.ExpireTimeSpan = TimeSpan.FromHours(10);
                cfg.EventsType = typeof(RestXCookieAuthenticationEvents);
                cfg.Events.OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.ToString().Contains("/api"))
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });
            //services.AddAuthorization(options =>
            //{
            //    var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder(
            //        "Cookies",
            //        "Bearer",
            //        "Identity.Application");
            //    defaultAuthorizationPolicyBuilder = defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();

            //    var entraId = new AuthorizationPolicyBuilder()
            //        .AddAuthenticationSchemes(OpenIdConnectDefaults.AuthenticationScheme)
            //        .RequireAuthenticatedUser()
            //        .Build();
            //    options.AddPolicy("Entra", entraId);
            //    options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();
            //});

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

            services.AddScoped<RestXCookieAuthenticationEvents>();

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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute("api", "api/{controller}/{action}/{id?}");
                //endpoints.MapFallbackToController("Index", "Public");
            });

        }

    }
}
