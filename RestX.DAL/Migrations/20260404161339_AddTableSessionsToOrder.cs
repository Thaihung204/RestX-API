using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddTableSessionsToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Reservations_ReservationId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_TableSessions_Orders_CurrentOrderId",
                table: "TableSessions");

            migrationBuilder.RenameColumn(
                name: "CurrentOrderId",
                table: "TableSessions",
                newName: "OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_TableSessions_CurrentOrderId",
                table: "TableSessions",
                newName: "IX_TableSessions_OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Reservations_ReservationId",
                table: "Payments",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TableSessions_Orders_OrderId",
                table: "TableSessions",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Reservations_ReservationId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_TableSessions_Orders_OrderId",
                table: "TableSessions");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "TableSessions",
                newName: "CurrentOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_TableSessions_OrderId",
                table: "TableSessions",
                newName: "IX_TableSessions_CurrentOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Reservations_ReservationId",
                table: "Payments",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TableSessions_Orders_CurrentOrderId",
                table: "TableSessions",
                column: "CurrentOrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
