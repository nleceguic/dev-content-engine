using DevContentEngine.Domain.Services;
using FluentAssertions;

namespace DevContentEngine.Domain.Tests.Services;

public class TechnologyExtractorTests
{
    [Fact]
    public void Extract_detects_CSharp_from_file_extension()
    {
        var extractor = new TechnologyExtractor();

        var result = extractor.Extract(["src/DevContentEngine.Domain/Entities/GeneratedPost.cs"], []);

        result.Should().Contain("C#");
    }

    [Fact]
    public void Extract_detects_dotnet_from_project_file_extension()
    {
        var extractor = new TechnologyExtractor();

        var result = extractor.Extract(["src/DevContentEngine.Domain/DevContentEngine.Domain.csproj"], []);

        result.Should().Contain(".NET");
    }

    [Fact]
    public void Extract_detects_docker_from_compose_file_path()
    {
        var extractor = new TechnologyExtractor();

        var result = extractor.Extract(["docker-compose.yml"], []);

        result.Should().Contain("Docker");
    }

    [Fact]
    public void Extract_detects_kafka_from_message_keyword()
    {
        var extractor = new TechnologyExtractor();

        var result = extractor.Extract([], ["Add Kafka consumer for activity events"]);

        result.Should().Contain("Kafka");
    }

    [Fact]
    public void Extract_detects_postgresql_from_sql_extension()
    {
        var extractor = new TechnologyExtractor();

        var result = extractor.Extract(["migrations/0001_initial.sql"], []);

        result.Should().Contain("PostgreSQL");
    }

    [Fact]
    public void Extract_combines_extension_and_path_signals_into_distinct_technologies()
    {
        var extractor = new TechnologyExtractor();

        var result = extractor.Extract(
            ["src/DevContentEngine.Worker/Worker.cs", "docker-compose.yml"],
            []);

        result.Should().BeEquivalentTo(["C#", "Docker"]);
    }

    [Fact]
    public void Extract_returns_empty_collection_when_nothing_matches()
    {
        var extractor = new TechnologyExtractor();

        var result = extractor.Extract(["README.md"], ["Update contributing guidelines"]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Extract_is_case_insensitive_for_extensions_paths_and_keywords()
    {
        var extractor = new TechnologyExtractor();

        var result = extractor.Extract(["Src/Worker/PROGRAM.CS", "DOCKERFILE"], ["POSTGRES migration"]);

        result.Should().BeEquivalentTo(["C#", "Docker", "PostgreSQL"]);
    }

    [Fact]
    public void Extract_does_not_duplicate_a_technology_matched_by_multiple_files()
    {
        var extractor = new TechnologyExtractor();

        var result = extractor.Extract(
            ["src/A.cs", "src/B.cs", "src/C.cs"],
            []);

        result.Should().ContainSingle().Which.Should().Be("C#");
    }

    [Fact]
    public void Extract_uses_a_custom_mapping_list_instead_of_the_defaults()
    {
        var extractor = new TechnologyExtractor([
            new TechnologyMapping("Terraform", fileExtensions: [".tf"])
        ]);

        var result = extractor.Extract(["infra/main.tf"], []);

        result.Should().BeEquivalentTo(["Terraform"]);
    }

    [Fact]
    public void Extract_detects_at_least_five_distinct_technologies_across_realistic_commit_data()
    {
        var extractor = new TechnologyExtractor();

        var result = extractor.Extract(
            filePaths:
            [
                "src/DevContentEngine.Domain/Entities/GeneratedPost.cs",
                "src/DevContentEngine.Domain/DevContentEngine.Domain.csproj",
                "docker-compose.yml",
                "migrations/0001_initial.sql"
            ],
            commitMessages: ["Wire up Kafka consumer and postgres migrations"]);

        result.Should().BeEquivalentTo(["C#", ".NET", "Docker", "Kafka", "PostgreSQL"]);
    }
}
