using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.AdminDAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepositConfigs",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinPartySize = table.Column<int>(type: "int", nullable: false),
                    DepositAmountPerPerson = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeadlineHours = table.Column<int>(type: "int", nullable: false),
                    EarlyRefundHours = table.Column<int>(type: "int", nullable: false),
                    EarlyRefundPercentage = table.Column<int>(type: "int", nullable: false),
                    LateRefundHours = table.Column<int>(type: "int", nullable: false),
                    LateRefundPercentage = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositConfigs", x => x.TenantId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepositConfigs");
        }
    }
}
