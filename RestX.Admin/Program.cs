using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Core.Types;
using RestX.BLL.Repository.Implementations;
using RestX.BLL.Repository.Interfaces;
using RestX.BLL.Services.Implementations;
using RestX.BLL.Services.Interfaces;
using RestX.DAL.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<RestxAdminContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionString")));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IRepository, EntityFrameworkRepository<RestxAdminContext>>();
builder.Services.AddScoped<ITenantService, TenantService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
