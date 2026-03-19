using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class MergeStaffRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET QUOTED_IDENTIFIER ON;
                SET ANSI_NULLS ON;

                DECLARE @StaffRoleId NVARCHAR(450) = NEWID();

                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'STAFF')
                BEGIN
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (@StaffRoleId, 'Staff', 'STAFF', NEWID());
                END
                ELSE
                BEGIN
                    SELECT @StaffRoleId = Id FROM AspNetRoles WHERE NormalizedName = 'STAFF';
                END

                DECLARE @KitchenStaffRoleId NVARCHAR(450);
                SELECT @KitchenStaffRoleId = Id FROM AspNetRoles WHERE NormalizedName = 'KITCHEN STAFF';

                IF @KitchenStaffRoleId IS NOT NULL
                BEGIN
                    INSERT INTO AspNetUserRoles (UserId, RoleId)
                    SELECT ur.UserId, @StaffRoleId
                    FROM AspNetUserRoles ur
                    WHERE ur.RoleId = @KitchenStaffRoleId
                      AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles ur2 WHERE ur2.UserId = ur.UserId AND ur2.RoleId = @StaffRoleId);

                    DELETE FROM AspNetUserRoles WHERE RoleId = @KitchenStaffRoleId;
                    DELETE FROM AspNetRoles WHERE Id = @KitchenStaffRoleId;
                END

                DECLARE @WaiterRoleId NVARCHAR(450);
                SELECT @WaiterRoleId = Id FROM AspNetRoles WHERE NormalizedName = 'WAITER';

                IF @WaiterRoleId IS NOT NULL
                BEGIN
                    INSERT INTO AspNetUserRoles (UserId, RoleId)
                    SELECT ur.UserId, @StaffRoleId
                    FROM AspNetUserRoles ur
                    WHERE ur.RoleId = @WaiterRoleId
                      AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles ur2 WHERE ur2.UserId = ur.UserId AND ur2.RoleId = @StaffRoleId);

                    DELETE FROM AspNetUserRoles WHERE RoleId = @WaiterRoleId;
                    DELETE FROM AspNetRoles WHERE Id = @WaiterRoleId;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET QUOTED_IDENTIFIER ON;
                SET ANSI_NULLS ON;

                DECLARE @StaffRoleId NVARCHAR(450);
                SELECT @StaffRoleId = Id FROM AspNetRoles WHERE NormalizedName = 'STAFF';

                DECLARE @KitchenStaffRoleId NVARCHAR(450) = NEWID();
                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'KITCHEN STAFF')
                BEGIN
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (@KitchenStaffRoleId, 'Kitchen Staff', 'KITCHEN STAFF', NEWID());
                END

                DECLARE @WaiterRoleId NVARCHAR(450) = NEWID();
                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'WAITER')
                BEGIN
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (@WaiterRoleId, 'Waiter', 'WAITER', NEWID());
                END

                IF @StaffRoleId IS NOT NULL
                BEGIN
                    DELETE FROM AspNetUserRoles WHERE RoleId = @StaffRoleId;
                    DELETE FROM AspNetRoles WHERE Id = @StaffRoleId;
                END
            ");
        }
    }
}
