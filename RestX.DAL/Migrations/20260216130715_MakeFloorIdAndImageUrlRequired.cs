using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestX.DAL.Migrations
{
    /// <inheritdoc />
    public partial class MakeFloorIdAndImageUrlRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tables_Floors_FloorId",
                table: "Tables");

            migrationBuilder.Sql(@"
                DELETE FROM TableSessions  WHERE TableId IN (SELECT Id FROM Tables WHERE FloorId IS NULL);
                DELETE FROM ReservationTables WHERE TableId IN (SELECT Id FROM Tables WHERE FloorId IS NULL);
                DELETE FROM OrderTables    WHERE TableId IN (SELECT Id FROM Tables WHERE FloorId IS NULL);
                DELETE FROM Tables         WHERE FloorId IS NULL;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "FloorId",
                table: "Tables",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Floors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tables_Floors_FloorId",
                table: "Tables",
                column: "FloorId",
                principalTable: "Floors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tables_Floors_FloorId",
                table: "Tables");

            migrationBuilder.AlterColumn<Guid>(
                name: "FloorId",
                table: "Tables",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Floors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddForeignKey(
                name: "FK_Tables_Floors_FloorId",
                table: "Tables",
                column: "FloorId",
                principalTable: "Floors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
