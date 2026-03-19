using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAITable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PropertiesJson",
                table: "AIChatSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PropertiesJson",
                table: "AIChatMessages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PropertiesJson",
                table: "AIChatSessions");

            migrationBuilder.DropColumn(
                name: "PropertiesJson",
                table: "AIChatMessages");
        }
    }
}
