using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevContentEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImagePromptToGeneratedPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePrompt",
                table: "GeneratedPosts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePrompt",
                table: "GeneratedPosts");
        }
    }
}
