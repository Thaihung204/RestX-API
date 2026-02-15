using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.AdminDAL.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsAcceptedWithTenantRequestStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAccepted",
                table: "TenantRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Hostname",
                table: "TenantRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tenantRequestStatus",
                table: "TenantRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tenantRequestStatus",
                table: "TenantRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Hostname",
                table: "TenantRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsAccepted",
                table: "TenantRequests",
                type: "bit",
                nullable: true);
        }
    }
}
