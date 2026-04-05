using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RX213_DropReservationTable_AddOrderToTableSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate existing ReservationTable data sang TableSession
            // Chỉ tạo session cho các reservation chưa có session nào (flow cũ)
            // Data migration: chuyển ReservationTables sang TableSessions
            // Chỉ chạy nếu bảng ReservationTables vẫn còn (chưa bị RefactorDb drop)
            migrationBuilder.Sql(@"
                IF OBJECT_ID('ReservationTables', 'U') IS NOT NULL
                BEGIN
                    INSERT INTO TableSessions (
                        Id, TableId, ReservationId, CurrentOrderId,
                        StartedAt, EndedAt, IsActive,
                        CreatedDate, CreatedBy, ModifiedDate, ModifiedBy, PropertiesJson
                    )
                    SELECT
                        NEWID(),
                        rt.TableId,
                        rt.ReservationId,
                        NULL,
                        r.Time,
                        CASE
                            WHEN rs.Code IN ('CANCELLED','COMPLETED','NO_SHOW') THEN DATEADD(HOUR, 1, r.Time)
                            ELSE NULL
                        END,
                        CASE
                            WHEN rs.Code IN ('CANCELLED','COMPLETED','NO_SHOW') THEN 0
                            ELSE 1
                        END,
                        GETUTCDATE(), 'migration', NULL, NULL, NULL
                    FROM ReservationTables rt
                    INNER JOIN Reservations r  ON r.Id  = rt.ReservationId
                    INNER JOIN StatusValues rs ON rs.Id = r.ReservationStatusId
                    WHERE NOT EXISTS (
                        SELECT 1 FROM TableSessions ts
                        WHERE ts.ReservationId = rt.ReservationId
                          AND ts.TableId       = rt.TableId
                    )
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservationTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PropertiesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationTables_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservationTables_Tables_TableId",
                        column: x => x.TableId,
                        principalTable: "Tables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationTables_ReservationId_TableId",
                table: "ReservationTables",
                columns: new[] { "ReservationId", "TableId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationTables_TableId",
                table: "ReservationTables",
                column: "TableId");
        }
    }
}
