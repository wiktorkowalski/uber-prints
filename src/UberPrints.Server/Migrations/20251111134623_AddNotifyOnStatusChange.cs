using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberPrints.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifyOnStatusChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnStatusChange",
                table: "PrintRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyOnStatusChange",
                table: "PrintRequests");
        }
    }
}
