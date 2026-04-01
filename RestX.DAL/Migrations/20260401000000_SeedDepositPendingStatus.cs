using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class SeedDepositPendingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DECLARE @reservationTypeId INT = (
                    SELECT Id FROM StatusTypes WHERE Code = 'RESERVATION'
                );

                IF @reservationTypeId IS NOT NULL AND NOT EXISTS (
                    SELECT 1 FROM StatusValues WHERE Code = 'DEPOSIT_PENDING' AND StatusTypeId = @reservationTypeId
                )
                BEGIN
                    INSERT INTO StatusValues (StatusTypeId, Code, Name, ColorCode, IsDefault, CreatedDate)
                    VALUES (@reservationTypeId, 'DEPOSIT_PENDING', 'Deposit Pending', '#FF9800', 0, GETDATE());
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM StatusValues WHERE Code = 'DEPOSIT_PENDING';
            ");
        }
    }
}
