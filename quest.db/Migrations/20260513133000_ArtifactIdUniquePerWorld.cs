using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quest.db.Migrations
{
    /// <inheritdoc />
    public partial class ArtifactIdUniquePerWorld : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_artifacts_ArtifactId",
                table: "artifacts");

            migrationBuilder.CreateIndex(
                name: "IX_artifacts_WorldId_ArtifactId",
                table: "artifacts",
                columns: new[] { "WorldId", "ArtifactId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_artifacts_WorldId_ArtifactId",
                table: "artifacts");

            migrationBuilder.CreateIndex(
                name: "IX_artifacts_ArtifactId",
                table: "artifacts",
                column: "ArtifactId",
                unique: true);
        }
    }
}
