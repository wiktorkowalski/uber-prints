using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberPrints.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestSessionTokenToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestSessionToken",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GuestSessionToken",
                table: "Users",
                column: "GuestSessionToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_GuestSessionToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GuestSessionToken",
                table: "Users");
        }
    }
}
