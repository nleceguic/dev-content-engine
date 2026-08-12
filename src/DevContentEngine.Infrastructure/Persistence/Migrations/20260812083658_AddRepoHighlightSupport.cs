using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevContentEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRepoHighlightSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RelatedRepositoryId",
                table: "ContentIdeas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentIdeas_RelatedRepositoryId",
                table: "ContentIdeas",
                column: "RelatedRepositoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentIdeas_GitHubRepositories_RelatedRepositoryId",
                table: "ContentIdeas",
                column: "RelatedRepositoryId",
                principalTable: "GitHubRepositories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentIdeas_GitHubRepositories_RelatedRepositoryId",
                table: "ContentIdeas");

            migrationBuilder.DropIndex(
                name: "IX_ContentIdeas_RelatedRepositoryId",
                table: "ContentIdeas");

            migrationBuilder.DropColumn(
                name: "RelatedRepositoryId",
                table: "ContentIdeas");
        }
    }
}
