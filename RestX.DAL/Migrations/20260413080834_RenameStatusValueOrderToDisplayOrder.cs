using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RenameStatusValueOrderToDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Order",
                table: "StatusValues",
                newName: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "StatusValues",
                newName: "Order");
        }
    }
}
