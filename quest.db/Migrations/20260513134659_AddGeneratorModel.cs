using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quest.db.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratorModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeneratorModel",
                table: "worlds",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratorModel",
                table: "worlds");
        }
    }
}
