using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberPrints.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPrinterSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedPrinterId",
                table: "PrintRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GCodeFileUrl",
                table: "PrintRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrintCompletedAt",
                table: "PrintRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrintJobId",
                table: "PrintRequests",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrintStartedAt",
                table: "PrintRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Printers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CurrentState = table.Column<string>(type: "text", nullable: false),
                    LastStatusUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    Capabilities = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NozzleTemperature = table.Column<double>(type: "double precision", nullable: true),
                    NozzleTargetTemperature = table.Column<double>(type: "double precision", nullable: true),
                    BedTemperature = table.Column<double>(type: "double precision", nullable: true),
                    BedTargetTemperature = table.Column<double>(type: "double precision", nullable: true),
                    PrintProgress = table.Column<int>(type: "integer", nullable: true),
                    TimeRemaining = table.Column<int>(type: "integer", nullable: true),
                    TimePrinting = table.Column<int>(type: "integer", nullable: true),
                    CurrentFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Printers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrinterStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    PrinterId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    NozzleTemperature = table.Column<double>(type: "double precision", nullable: true),
                    BedTemperature = table.Column<double>(type: "double precision", nullable: true),
                    PrintProgress = table.Column<int>(type: "integer", nullable: true),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrinterStatusHistories_Printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "Printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrintRequests_AssignedPrinterId",
                table: "PrintRequests",
                column: "AssignedPrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_PrinterStatusHistories_PrinterId",
                table: "PrinterStatusHistories",
                column: "PrinterId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrintRequests_Printers_AssignedPrinterId",
                table: "PrintRequests",
                column: "AssignedPrinterId",
                principalTable: "Printers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrintRequests_Printers_AssignedPrinterId",
                table: "PrintRequests");

            migrationBuilder.DropTable(
                name: "PrinterStatusHistories");

            migrationBuilder.DropTable(
                name: "Printers");

            migrationBuilder.DropIndex(
                name: "IX_PrintRequests_AssignedPrinterId",
                table: "PrintRequests");

            migrationBuilder.DropColumn(
                name: "AssignedPrinterId",
                table: "PrintRequests");

            migrationBuilder.DropColumn(
                name: "GCodeFileUrl",
                table: "PrintRequests");

            migrationBuilder.DropColumn(
                name: "PrintCompletedAt",
                table: "PrintRequests");

            migrationBuilder.DropColumn(
                name: "PrintJobId",
                table: "PrintRequests");

            migrationBuilder.DropColumn(
                name: "PrintStartedAt",
                table: "PrintRequests");
        }
    }
}
