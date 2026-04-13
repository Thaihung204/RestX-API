using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderToStatusValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add Order column with default 1 (so NOT NULL constraint passes)
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "StatusValues",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Step 2: Assign correct Order based on seeder insertion order
            migrationBuilder.Sql(@"
                -- RESERVATION: PENDING=1, CONFIRMED=2, CANCELLED=3
                UPDATE sv SET sv.[Order] = CASE sv.Code
                    WHEN 'PENDING'   THEN 1
                    WHEN 'CONFIRMED' THEN 2
                    WHEN 'CANCELLED' THEN 3
                    ELSE 1
                END
                FROM StatusValues sv
                JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                WHERE st.Code = 'RESERVATION';

                -- ORDER-DETAIL: PREPARING=1, SERVED=2, CANCELLED=3
                UPDATE sv SET sv.[Order] = CASE sv.Code
                    WHEN 'PREPARING' THEN 1
                    WHEN 'SERVED'    THEN 2
                    WHEN 'CANCELLED' THEN 3
                    ELSE 1
                END
                FROM StatusValues sv
                JOIN StatusTypes st ON sv.StatusTypeId = st.Id
                WHERE st.Code = 'ORDER-DETAIL';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Order", table: "StatusValues");
        }
    }
}
