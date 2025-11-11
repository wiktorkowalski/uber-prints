using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberPrints.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPrinterExtendedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AxisX",
                table: "Printers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AxisY",
                table: "Printers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AxisZ",
                table: "Printers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FanHotend",
                table: "Printers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FanPrint",
                table: "Printers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FlowRate",
                table: "Printers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpeedRate",
                table: "Printers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AxisX",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "AxisY",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "AxisZ",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "FanHotend",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "FanPrint",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "FlowRate",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "SpeedRate",
                table: "Printers");
        }
    }
}
