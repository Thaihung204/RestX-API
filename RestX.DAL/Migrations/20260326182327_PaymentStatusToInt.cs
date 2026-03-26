using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class PaymentStatusToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Thêm cột int tạm
            migrationBuilder.AddColumn<int>(
                name: "StatusInt",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 2. Convert string → int (theo thứ tự enum: Unpaid=0, Paid=1, Cancelled=2, Refunded=3)
            migrationBuilder.Sql(@"
                UPDATE Payments SET StatusInt = CASE Status
                    WHEN 'Paid'      THEN 1
                    WHEN 'Cancelled' THEN 2
                    WHEN 'Refunded'  THEN 3
                    ELSE 0
                END
            ");

            // 3. Drop cột string cũ
            migrationBuilder.DropColumn(name: "Status", table: "Payments");

            // 4. Đổi tên StatusInt → Status
            migrationBuilder.RenameColumn(
                name: "StatusInt",
                table: "Payments",
                newName: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
