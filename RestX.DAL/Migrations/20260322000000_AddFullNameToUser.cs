using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    public partial class AddFullNameToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            // Data migration: copy existing UserName (which stored FullName) into the new FullName column
            migrationBuilder.Sql("UPDATE AspNetUsers SET FullName = UserName WHERE FullName IS NULL");

            // After data migration, enforce NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            // Update UserName to use Email as unique identifier (for users who have email)
            // Exclude Tenant Admin and System Admin — they keep their original UserName
            migrationBuilder.Sql(@"
                UPDATE u
                SET u.UserName = u.Email, u.NormalizedUserName = UPPER(u.Email)
                FROM AspNetUsers u
                WHERE u.Email IS NOT NULL AND u.Email != '' AND LEN(u.Email) > 0
                  AND u.Id NOT IN (
                      SELECT ur.UserId
                      FROM AspNetUserRoles ur
                      INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                      WHERE r.Name IN ('Tenant Admin', 'System Admin')
                  )
            ");

            // Update UserName to use PhoneNumber for phone-only users (no email)
            // Exclude Tenant Admin and System Admin
            migrationBuilder.Sql(@"
                UPDATE u
                SET u.UserName = u.PhoneNumber, u.NormalizedUserName = u.PhoneNumber
                FROM AspNetUsers u
                WHERE (u.Email IS NULL OR u.Email = '') AND u.PhoneNumber IS NOT NULL AND u.PhoneNumber != ''
                  AND u.Id NOT IN (
                      SELECT ur.UserId
                      FROM AspNetUserRoles ur
                      INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                      WHERE r.Name IN ('Tenant Admin', 'System Admin')
                  )
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore UserName = FullName before dropping the column
            migrationBuilder.Sql("UPDATE AspNetUsers SET UserName = FullName, NormalizedUserName = UPPER(FullName) WHERE FullName IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "AspNetUsers");
        }
    }
}
