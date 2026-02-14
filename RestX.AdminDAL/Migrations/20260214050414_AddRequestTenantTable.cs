using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.AdminDAL.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestTenantTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hostname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessPrimaryPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessEmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessAddressLine1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessAddressLine2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessAddressLine3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessAddressLine4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessCountry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRequests", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantRequests");
        }
    }
}
