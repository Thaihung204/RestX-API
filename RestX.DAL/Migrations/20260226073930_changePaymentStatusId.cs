using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class changePaymentStatusId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_StatusValues_PaymentStatusId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentStatusId",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatusId1",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentStatusId1",
                table: "Orders",
                column: "PaymentStatusId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_StatusValues_PaymentStatusId1",
                table: "Orders",
                column: "PaymentStatusId1",
                principalTable: "StatusValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_StatusValues_PaymentStatusId1",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentStatusId1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentStatusId1",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentStatusId",
                table: "Orders",
                column: "PaymentStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_StatusValues_PaymentStatusId",
                table: "Orders",
                column: "PaymentStatusId",
                principalTable: "StatusValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
