using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class DropPaymentStatusForeignKey : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
