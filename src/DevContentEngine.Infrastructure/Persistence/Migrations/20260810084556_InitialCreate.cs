using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevContentEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GitHubRepositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubRepositories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RelevanceScore = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trends", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GitHubActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DetectedTechnologies = table.Column<string[]>(type: "text[]", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsNoise = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitHubActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GitHubActivities_GitHubRepositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "GitHubRepositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentIdeas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Origin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActivityScore = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    RelatedActivityIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    RelatedTrendId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChosenPath = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentIdeas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentIdeas_Trends_RelatedTrendId",
                        column: x => x.RelatedTrendId,
                        principalTable: "Trends",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentIdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Hook = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Conclusion = table.Column<string>(type: "text", nullable: false),
                    Cta = table.Column<string>(type: "text", nullable: true),
                    Hashtags = table.Column<string[]>(type: "text[]", nullable: false),
                    Sources = table.Column<string[]>(type: "text[]", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Origin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PromptVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedPosts_ContentIdeas_ContentIdeaId",
                        column: x => x.ContentIdeaId,
                        principalTable: "ContentIdeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeneratedPosts_PromptVersions_PromptVersionId",
                        column: x => x.PromptVersionId,
                        principalTable: "PromptVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GenerationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChosenPath = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TokensUsed = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ResultingPostId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenerationRuns_GeneratedPosts_ResultingPostId",
                        column: x => x.ResultingPostId,
                        principalTable: "GeneratedPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PublishedPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EngagementNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublishedPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublishedPosts_GeneratedPosts_GeneratedPostId",
                        column: x => x.GeneratedPostId,
                        principalTable: "GeneratedPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentIdeas_RelatedTrendId",
                table: "ContentIdeas",
                column: "RelatedTrendId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedPosts_ContentIdeaId",
                table: "GeneratedPosts",
                column: "ContentIdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedPosts_CreatedAt",
                table: "GeneratedPosts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedPosts_Hashtags",
                table: "GeneratedPosts",
                column: "Hashtags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedPosts_PromptVersionId",
                table: "GeneratedPosts",
                column: "PromptVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationRuns_ResultingPostId",
                table: "GenerationRuns",
                column: "ResultingPostId");

            migrationBuilder.CreateIndex(
                name: "IX_GitHubActivities_ExternalId",
                table: "GitHubActivities",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GitHubActivities_RepositoryId_Timestamp",
                table: "GitHubActivities",
                columns: new[] { "RepositoryId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_GitHubRepositories_Owner_Name",
                table: "GitHubRepositories",
                columns: new[] { "Owner", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublishedPosts_GeneratedPostId",
                table: "PublishedPosts",
                column: "GeneratedPostId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GenerationRuns");

            migrationBuilder.DropTable(
                name: "GitHubActivities");

            migrationBuilder.DropTable(
                name: "PublishedPosts");

            migrationBuilder.DropTable(
                name: "GitHubRepositories");

            migrationBuilder.DropTable(
                name: "GeneratedPosts");

            migrationBuilder.DropTable(
                name: "ContentIdeas");

            migrationBuilder.DropTable(
                name: "PromptVersions");

            migrationBuilder.DropTable(
                name: "Trends");
        }
    }
}
