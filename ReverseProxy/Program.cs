using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost
});
app.Use(async (ctx, next) =>
{
    Console.WriteLine("HOST = " + ctx.Request.Host);
    Console.WriteLine("XFH = " + ctx.Request.Headers["X-Forwarded-Host"]);
    await next();
});

//app.UseHttpsRedirection();
app.UseRouting();
app.MapReverseProxy();

app.Run("http://0.0.0.0:80");
