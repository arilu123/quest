using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quest.db.Migrations
{
    /// <inheritdoc />
    public partial class FateToArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fate",
                table: "worlds");

            migrationBuilder.AddColumn<string>(
                name: "Fates",
                table: "worlds",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fates",
                table: "worlds");

            migrationBuilder.AddColumn<string>(
                name: "Fate",
                table: "worlds",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }
    }
}
