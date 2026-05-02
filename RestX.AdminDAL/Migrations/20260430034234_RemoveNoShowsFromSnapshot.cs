using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.AdminDAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNoShowsFromSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoShows",
                table: "TenantSnapshots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NoShows",
                table: "TenantSnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
