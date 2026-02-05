using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.AdminDAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AboutUs",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AboutUs",
                table: "Tenants");
        }
    }
}
