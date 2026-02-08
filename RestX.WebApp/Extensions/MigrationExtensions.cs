using Microsoft.EntityFrameworkCore;
using RestX.DAL.Context;

namespace RestX.WebApp.Extensions
{
    public static class MigrationExtensions
    {
        public static void ApplyMigrations(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<TenantDbContext>();

            db.Database.Migrate();
        }
    }

}
