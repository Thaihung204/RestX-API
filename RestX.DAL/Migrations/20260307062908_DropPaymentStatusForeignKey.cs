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
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_Orders_StatusValues_PaymentStatusId'
                )
                BEGIN
                    ALTER TABLE Orders DROP CONSTRAINT FK_Orders_StatusValues_PaymentStatusId;
                END

                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_Orders_PaymentStatusId' AND object_id = OBJECT_ID('Orders')
                )
                BEGIN
                    DROP INDEX IX_Orders_PaymentStatusId ON Orders;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
