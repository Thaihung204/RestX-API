using AspNetCoreRateLimit;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpLogging;
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
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.BLL.MultiTenancy;
using RestX.BLL.Services;
using RestX.DAL.Context;
using RestX.Models.Identity;
using RestX.Models.Tenants;
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
            services.AddScoped<IRedisService, RedisService>();
            services.AddMultitenancy<ActiveTenant, TenantResolver>();
            //services.Configure<RazorViewEngineOptions>(
            //    options => { options.ViewLocationExpanders.Add(new TenantViewLocationExpander()); });

            // Add framework services.
            services.AddDbContext<RestxAdminContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString("AdminDbContext"), options => options.EnableRetryOnFailure()));
            services.AddDbContext<RestaurantDbContext>();

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<RestaurantDbContext>().AddDefaultTokenProviders();

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
            services.AddAuthorization(options =>
            {
                var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder(
                    "Cookies",
                    "Bearer",
                    "Identity.Application");
                defaultAuthorizationPolicyBuilder = defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();

                var entraId = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(OpenIdConnectDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
                options.AddPolicy("Entra", entraId);
                options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();
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

            services.AddScoped<RestXCookieAuthenticationEvents>();

            //services.AddWebOptimizer(BundleHelper.RegisterBundles);
            services.AddAutoMapper(typeof(AutoMapperProfile));

            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            //services.Configure<AzureAdOptions>(Configuration.GetSection("AzureAd"));
            services.Configure<ConnectionStrings>(Configuration.GetSection("ConnectionStrings"));
            services.AddResponseCompression();

            SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler
            {
                // Customize this value based on desired DNS refresh timer
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };
            // Registering the Singleton SocketsHttpHandler lets you reuse it across any HttpClient in your application
            services.AddSingleton<SocketsHttpHandler>(socketsHttpHandler);
            services.AddSignalR();
            services.AddScoped<ITenantService, TenantService>();
            services.AddScoped<IRepository, EntityFrameworkRepository<RestxAdminContext>>();
            services.AddSingleton<IMemoryCache, MemoryCache>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddTransient<TelemetryExtender>();

            // Feature Management
            //services.AddSingleton<IFeatureDefinitionProvider, TenantBrandFeatureProvider>()
            //    .AddFeatureManagement().UseDisabledFeaturesHandler(new DisabledFeaturesHandler());

            isDevlopement = isDevlopement || (Configuration.GetSection("AppSettings")["EmailProvider"] ?? "") == "Mailtrap";
            DIHelper.Setup(services, isDevlopement);
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new OpenApiInfo
                    {
                        Title = "RestX API",
                        Version = "v1",
                        Description = "The documentation below is provided to help you integrate directly with RestX using Open API Standards.",
                        Contact = new OpenApiContact()
                        {
                            Name = "RestX Support",
                            Email = "support@restx.co.uk",
                            Url = new Uri("https://www.restx.co.uk/")
                        }

                    });

                //c.EnableAnnotations();
                c.CustomSchemaIds(type => type.FullName);
                // Set the comments path for the Swagger JSON and UI.
                // Need to import the XML schema for the BLL to show property decorators from there.
                //c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "RestX.WebApp.xml"), false);
                //c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "RestX.BLL.xml"), false);


                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                });
                //c.OperationFilter<AddAuthHeaderOperationFilter>();

                c.TagActionsBy(api =>
                {
                    if (api.GroupName != null)
                    {
                        return new[] { api.GroupName };
                    }

                    var controllerActionDescriptor = api.ActionDescriptor as ControllerActionDescriptor;
                    if (controllerActionDescriptor != null)
                    {
                        return new[] { controllerActionDescriptor.ControllerName };
                    }

                    throw new InvalidOperationException("Unable to determine tag for endpoint.");
                });
                c.DocInclusionPredicate((name, api) => true);

                c.OrderActionsBy(apiDesc =>
                {
                    return apiDesc.HttpMethod == "GET" ? "1" : "2";
                });
                //c.DocumentFilter<TagReOrderDocumentFilter>();
                //c.DocumentFilter<DupePathRoutesFilter>();
                //c.DocumentFilter<IVectorFilter>();
                //c.DocumentFilter<BasePathDocumentFilter>();
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

            app.UseMultitenancy<ActiveTenant>();
            app.UseMiddleware<TenantUnresolvedRedirectMiddleware<ActiveTenant>>("https://www.tprofile.co.uk/", false);
            app.UseMiddleware<TenantRedirectMiddleware<ActiveTenant>>();
            //app.UseIpRateLimiting();
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None, // Needs to be none so we can set cookies for brands with multiple domains
            });
            app.Use((context, next) =>
            {
                context.Response.Headers.Add("X-Endpoint",
                    Environment.GetEnvironmentVariable("APPSETTING_AppServiceId") ?? "Not Available");
                return next.Invoke();
            });

            app.UseResponseCompression();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.InjectStylesheet("/css/swagger-style.css");
                c.InjectJavascript("/js/swagger.js");
                c.RoutePrefix = "api-documentation";
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tprofile API v1");
                c.DefaultModelsExpandDepth(-1);
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<TelemetryExtender>();

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapControllerRoute("robots", "robots.txt", new { controller = "AppResourceLoader", action = "Robots" });
            //    endpoints.MapControllerRoute("login-page", "login", new { controller = "Login", action = "Index" });
            //    endpoints.MapControllerRoute("logout-page", "logout", new { controller = "Logout", action = "Index" });
            //    endpoints.MapControllerRoute("register-page", "register", new { controller = "Register", action = "Index" });
            //    endpoints.MapControllerRoute("no-route", "", new { controller = "Home", action = "Index" });
            //    endpoints.MapControllerRoute("default", "{contentUrl}", new { controller = "Home", action = "Index" });
            //    endpoints.MapControllers();
            //    endpoints.MapControllerRoute("api", "api/{controller}/{action}/{id?}");
            //    endpoints.MapFallbackToController("Index", "Public");
            //});
        }

    }
}
