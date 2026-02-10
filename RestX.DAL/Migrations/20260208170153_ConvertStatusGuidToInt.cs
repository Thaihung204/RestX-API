using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ConvertStatusGuidToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // STEP 1: Drop ALL FK constraints that reference StatusValues
            // ============================================================
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_StatusValues_OrderStatusId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Tables_StatusValues_TableStatusId",
                table: "Tables");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_StatusValues_PaymentStatusId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_StatusValues_ItemStatusId",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_StatusValues_PaymentStatusId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_StatusValues_ReservationStatusId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeSchedules_StatusValues_StatusId",
                table: "EmployeeSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_StatusValues_StatusTypes_StatusTypeId",
                table: "StatusValues");

            // ============================================================
            // STEP 2: Drop indexes on FK columns
            // ============================================================
            migrationBuilder.DropIndex(
                name: "IX_Tables_TableStatusId",
                table: "Tables");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderStatusId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentStatusId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_ItemStatusId",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentStatusId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_ReservationStatusId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeSchedules_StatusId",
                table: "EmployeeSchedules");

            // ============================================================
            // STEP 3: GROUP 1 - Convert enum-based statuses (Table, Order)
            // Map GUID → enum int using StatusValues.Code
            // ============================================================

            // --- Tables.TableStatusId ---
            migrationBuilder.AddColumn<int>(
                name: "TableStatusId_New",
                table: "Tables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE t SET t.TableStatusId_New =
                    CASE sv.Code
                        WHEN 'AVAILABLE' THEN 0
                        WHEN 'RESERVED' THEN 1
                        WHEN 'OCCUPIED' THEN 2
                        ELSE 0
                    END
                FROM [Tables] t
                INNER JOIN [StatusValues] sv ON t.TableStatusId = sv.Id
            ");

            migrationBuilder.DropColumn(name: "TableStatusId", table: "Tables");
            migrationBuilder.RenameColumn(name: "TableStatusId_New", table: "Tables", newName: "TableStatusId");

            // --- Orders.OrderStatusId ---
            migrationBuilder.AddColumn<int>(
                name: "OrderStatusId_New",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE o SET o.OrderStatusId_New =
                    CASE sv.Code
                        WHEN 'RESERVED' THEN 0
                        WHEN 'SERVING' THEN 1
                        WHEN 'COMPLETED' THEN 2
                        WHEN 'DELETED' THEN 3
                        ELSE 0
                    END
                FROM [Orders] o
                INNER JOIN [StatusValues] sv ON o.OrderStatusId = sv.Id
            ");

            migrationBuilder.DropColumn(name: "OrderStatusId", table: "Orders");
            migrationBuilder.RenameColumn(name: "OrderStatusId_New", table: "Orders", newName: "OrderStatusId");

            // ============================================================
            // STEP 4: GROUP 2 - Convert DB-backed statuses
            // StatusTypes & StatusValues: GUID PK → int identity PK
            // ============================================================

            // Create temp mapping table for StatusTypes (old GUID → new int)
            // Using real tables instead of #temp tables to ensure persistence across migration commands
            migrationBuilder.Sql(@"
                CREATE TABLE [__StatusTypeMap] (
                    OldId uniqueidentifier NOT NULL,
                    NewId int IDENTITY(1,1) NOT NULL
                );
                INSERT INTO [__StatusTypeMap] (OldId)
                SELECT Id FROM [StatusTypes] ORDER BY Code;
            ");

            // Create temp mapping table for StatusValues (old GUID → new int)
            migrationBuilder.Sql(@"
                CREATE TABLE [__StatusValueMap] (
                    OldId uniqueidentifier NOT NULL,
                    NewId int IDENTITY(1,1) NOT NULL
                );
                INSERT INTO [__StatusValueMap] (OldId)
                SELECT Id FROM [StatusValues] ORDER BY StatusTypeId, Code;
            ");

            // --- Update FK columns in entity tables using mapping ---

            // Orders.PaymentStatusId
            migrationBuilder.AddColumn<int>(
                name: "PaymentStatusId_New",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE o SET o.PaymentStatusId_New = ISNULL(m.NewId, 0)
                FROM [Orders] o
                LEFT JOIN [__StatusValueMap] m ON o.PaymentStatusId = m.OldId
            ");

            migrationBuilder.DropColumn(name: "PaymentStatusId", table: "Orders");
            migrationBuilder.RenameColumn(name: "PaymentStatusId_New", table: "Orders", newName: "PaymentStatusId");

            // OrderDetails.ItemStatusId
            migrationBuilder.AddColumn<int>(
                name: "ItemStatusId_New",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE od SET od.ItemStatusId_New = ISNULL(m.NewId, 0)
                FROM [OrderDetails] od
                LEFT JOIN [__StatusValueMap] m ON od.ItemStatusId = m.OldId
            ");

            migrationBuilder.DropColumn(name: "ItemStatusId", table: "OrderDetails");
            migrationBuilder.RenameColumn(name: "ItemStatusId_New", table: "OrderDetails", newName: "ItemStatusId");

            // Payments.PaymentStatusId
            migrationBuilder.AddColumn<int>(
                name: "PaymentStatusId_New",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE p SET p.PaymentStatusId_New = ISNULL(m.NewId, 0)
                FROM [Payments] p
                LEFT JOIN [__StatusValueMap] m ON p.PaymentStatusId = m.OldId
            ");

            migrationBuilder.DropColumn(name: "PaymentStatusId", table: "Payments");
            migrationBuilder.RenameColumn(name: "PaymentStatusId_New", table: "Payments", newName: "PaymentStatusId");

            // Reservations.ReservationStatusId
            migrationBuilder.AddColumn<int>(
                name: "ReservationStatusId_New",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE r SET r.ReservationStatusId_New = ISNULL(m.NewId, 0)
                FROM [Reservations] r
                LEFT JOIN [__StatusValueMap] m ON r.ReservationStatusId = m.OldId
            ");

            migrationBuilder.DropColumn(name: "ReservationStatusId", table: "Reservations");
            migrationBuilder.RenameColumn(name: "ReservationStatusId_New", table: "Reservations", newName: "ReservationStatusId");

            // EmployeeSchedules.StatusId
            migrationBuilder.AddColumn<int>(
                name: "StatusId_New",
                table: "EmployeeSchedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE es SET es.StatusId_New = ISNULL(m.NewId, 0)
                FROM [EmployeeSchedules] es
                LEFT JOIN [__StatusValueMap] m ON es.StatusId = m.OldId
            ");

            migrationBuilder.DropColumn(name: "StatusId", table: "EmployeeSchedules");
            migrationBuilder.RenameColumn(name: "StatusId_New", table: "EmployeeSchedules", newName: "StatusId");

            // ============================================================
            // STEP 5: Rebuild StatusValues table with int PK
            // ============================================================
            migrationBuilder.Sql(@"
                -- Drop indexes on StatusValues
                DROP INDEX IF EXISTS IX_StatusValues_StatusTypeId_Code ON [StatusValues];

                -- Create new StatusValues table with int PK
                CREATE TABLE [StatusValues_New] (
                    [Id] int IDENTITY(1,1) NOT NULL,
                    [StatusTypeId] int NOT NULL,
                    [Code] nvarchar(50) NOT NULL,
                    [Name] nvarchar(255) NOT NULL,
                    [ColorCode] nvarchar(7) NULL,
                    [IsDefault] bit NOT NULL DEFAULT 0,
                    [CreatedDate] datetime2 NOT NULL,
                    [ModifiedDate] datetime2 NULL,
                    [CreatedBy] nvarchar(100) NULL,
                    [ModifiedBy] nvarchar(100) NULL,
                    CONSTRAINT [PK_StatusValues_New] PRIMARY KEY ([Id])
                );

                -- Insert data preserving the mapped int IDs
                SET IDENTITY_INSERT [StatusValues_New] ON;
                INSERT INTO [StatusValues_New] ([Id], [StatusTypeId], [Code], [Name], [ColorCode], [IsDefault], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
                SELECT m.NewId, tm.NewId, sv.Code, sv.Name, sv.ColorCode, 0, sv.CreatedDate, sv.ModifiedDate, sv.CreatedBy, sv.ModifiedBy
                FROM [StatusValues] sv
                INNER JOIN [__StatusValueMap] m ON sv.Id = m.OldId
                INNER JOIN [__StatusTypeMap] tm ON sv.StatusTypeId = tm.OldId;
                SET IDENTITY_INSERT [StatusValues_New] OFF;

                -- Drop old table and rename
                DROP TABLE [StatusValues];
                EXEC sp_rename 'StatusValues_New', 'StatusValues';
                EXEC sp_rename 'PK_StatusValues_New', 'PK_StatusValues', 'OBJECT';
            ");

            // ============================================================
            // STEP 6: Rebuild StatusTypes table with int PK
            // ============================================================
            migrationBuilder.Sql(@"
                -- Drop index on StatusTypes
                DROP INDEX IF EXISTS IX_StatusTypes_Code ON [StatusTypes];

                -- Create new StatusTypes table with int PK
                CREATE TABLE [StatusTypes_New] (
                    [Id] int IDENTITY(1,1) NOT NULL,
                    [Code] nvarchar(50) NOT NULL,
                    [CreatedDate] datetime2 NOT NULL,
                    [ModifiedDate] datetime2 NULL,
                    [CreatedBy] nvarchar(100) NULL,
                    [ModifiedBy] nvarchar(100) NULL,
                    CONSTRAINT [PK_StatusTypes_New] PRIMARY KEY ([Id])
                );

                -- Insert data preserving the mapped int IDs
                SET IDENTITY_INSERT [StatusTypes_New] ON;
                INSERT INTO [StatusTypes_New] ([Id], [Code], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
                SELECT m.NewId, st.Code, st.CreatedDate, st.ModifiedDate, st.CreatedBy, st.ModifiedBy
                FROM [StatusTypes] st
                INNER JOIN [__StatusTypeMap] m ON st.Id = m.OldId;
                SET IDENTITY_INSERT [StatusTypes_New] OFF;

                -- Drop old table and rename
                DROP TABLE [StatusTypes];
                EXEC sp_rename 'StatusTypes_New', 'StatusTypes';
                EXEC sp_rename 'PK_StatusTypes_New', 'PK_StatusTypes', 'OBJECT';
            ");

            // ============================================================
            // STEP 6.5: Set IsDefault for initial statuses
            // Must run after both tables are rebuilt with int PKs
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE sv SET sv.IsDefault = 1
                FROM [StatusValues] sv
                INNER JOIN [StatusTypes] st ON sv.StatusTypeId = st.Id
                WHERE (st.Code = 'TABLE_STATUS' AND sv.Code = 'AVAILABLE')
                   OR (st.Code = 'ORDER_STATUS' AND sv.Code = 'RESERVED')
                   OR (st.Code = 'PAYMENT_STATUS' AND sv.Code = 'UNPAID')
                   OR (st.Code = 'RESERVATION_STATUS' AND sv.Code = 'PENDING')
                   OR (st.Code = 'ITEM_STATUS' AND sv.Code = 'PENDING');
            ");

            // ============================================================
            // STEP 7: Clean up temp tables
            // ============================================================
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS [__StatusValueMap];
                DROP TABLE IF EXISTS [__StatusTypeMap];
            ");

            // ============================================================
            // STEP 9: Recreate indexes and FK constraints (Group 2 only)
            // ============================================================
            migrationBuilder.CreateIndex(
                name: "IX_StatusTypes_Code",
                table: "StatusTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusValues_StatusTypeId_Code",
                table: "StatusValues",
                columns: new[] { "StatusTypeId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentStatusId",
                table: "Orders",
                column: "PaymentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ItemStatusId",
                table: "OrderDetails",
                column: "ItemStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentStatusId",
                table: "Payments",
                column: "PaymentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ReservationStatusId",
                table: "Reservations",
                column: "ReservationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSchedules_StatusId",
                table: "EmployeeSchedules",
                column: "StatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_StatusValues_StatusTypes_StatusTypeId",
                table: "StatusValues",
                column: "StatusTypeId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_StatusValues_PaymentStatusId",
                table: "Orders",
                column: "PaymentStatusId",
                principalTable: "StatusValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_StatusValues_ItemStatusId",
                table: "OrderDetails",
                column: "ItemStatusId",
                principalTable: "StatusValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_StatusValues_PaymentStatusId",
                table: "Payments",
                column: "PaymentStatusId",
                principalTable: "StatusValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_StatusValues_ReservationStatusId",
                table: "Reservations",
                column: "ReservationStatusId",
                principalTable: "StatusValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeSchedules_StatusValues_StatusId",
                table: "EmployeeSchedules",
                column: "StatusId",
                principalTable: "StatusValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration is complex and would require reverse mapping.
            // In practice, restore from backup if rollback is needed.
            throw new NotSupportedException("This migration cannot be reversed. Restore from backup if needed.");
        }
    }
}
