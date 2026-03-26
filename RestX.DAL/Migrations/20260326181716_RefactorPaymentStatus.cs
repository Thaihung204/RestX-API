using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Thêm Status column nullable trước
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // 2. Convert data cũ từ StatusValues sang enum string
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.Status = CASE sv.Code
                    WHEN 'PAID'      THEN 'Paid'
                    WHEN 'CANCELLED' THEN 'Cancelled'
                    WHEN 'REFUNDED'  THEN 'Refunded'
                    ELSE             'Unpaid'
                END
                FROM Payments p
                LEFT JOIN StatusValues sv ON sv.Id = p.PaymentStatusId
            ");

            // 3. Đổi Status thành NOT NULL sau khi đã có data
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unpaid",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            // 4. Drop FK, index, column cũ
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_StatusValues_PaymentStatusId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentStatusId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentStatusId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentStatusId",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatusId",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatusId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentStatusId",
                table: "Payments",
                column: "PaymentStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_StatusValues_PaymentStatusId",
                table: "Payments",
                column: "PaymentStatusId",
                principalTable: "StatusValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
