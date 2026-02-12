using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.AdminDAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTenantColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SecondaryColor",
                table: "Tenants",
                newName: "LightSurfaceColor");

            migrationBuilder.RenameColumn(
                name: "HeaderColor",
                table: "Tenants",
                newName: "LightCardColor");

            migrationBuilder.RenameColumn(
                name: "FooterColor",
                table: "Tenants",
                newName: "LightBaseColor");

            migrationBuilder.RenameColumn(
                name: "BaseColor",
                table: "Tenants",
                newName: "DarkSurfaceColor");

            migrationBuilder.AddColumn<string>(
                name: "DarkBaseColor",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DarkCardColor",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DarkBaseColor",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DarkCardColor",
                table: "Tenants");

            migrationBuilder.RenameColumn(
                name: "LightSurfaceColor",
                table: "Tenants",
                newName: "SecondaryColor");

            migrationBuilder.RenameColumn(
                name: "LightCardColor",
                table: "Tenants",
                newName: "HeaderColor");

            migrationBuilder.RenameColumn(
                name: "LightBaseColor",
                table: "Tenants",
                newName: "FooterColor");

            migrationBuilder.RenameColumn(
                name: "DarkSurfaceColor",
                table: "Tenants",
                newName: "BaseColor");
        }
    }
}
