using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quest.db.Migrations
{
    /// <inheritdoc />
    public partial class AddArtifactNameAndId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArtifactId",
                table: "artifacts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "artifacts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_artifacts_ArtifactId",
                table: "artifacts",
                column: "ArtifactId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_artifacts_ArtifactId",
                table: "artifacts");

            migrationBuilder.DropColumn(
                name: "ArtifactId",
                table: "artifacts");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "artifacts");
        }
    }
}
