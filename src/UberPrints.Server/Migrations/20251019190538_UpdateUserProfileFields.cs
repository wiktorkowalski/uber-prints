using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UberPrints.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Users",
                newName: "AvatarHash");

            migrationBuilder.AddColumn<string>(
                name: "GlobalName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GlobalName",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "AvatarHash",
                table: "Users",
                newName: "Email");
        }
    }
}
