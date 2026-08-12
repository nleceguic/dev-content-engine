using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Services;
using DevContentEngine.Infrastructure.Llm.Prompts;
using FluentAssertions;

namespace DevContentEngine.Infrastructure.Tests.Llm;

public class GeneratorPromptBuilderTests
{
    private static readonly DateTime Now = new(2026, 8, 12, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildSystemPrompt_bans_flat_announcement_openings()
    {
        var prompt = GeneratorPromptBuilder.BuildSystemPrompt();

        prompt.Should().Contain("Acabo de subir a GitHub el proyecto que");
        prompt.Should().Contain("Hoy quiero compartir");
    }

    [Fact]
    public void BuildSystemPrompt_requires_a_tension_or_contrast_hook()
    {
        var prompt = GeneratorPromptBuilder.BuildSystemPrompt();

        prompt.Should().Contain("tensión");
        prompt.Should().Contain("contraste");
        prompt.Should().Contain("La mayoría de");
        prompt.Should().Contain("Podría haberme quedado en");
    }

    [Fact]
    public void BuildSystemPrompt_requires_bullet_emoji_for_multiple_technical_decisions()
    {
        var prompt = GeneratorPromptBuilder.BuildSystemPrompt();

        prompt.Should().Contain("🔹");
        prompt.Should().Contain("2 a 4 bullets");
    }

    [Fact]
    public void BuildSystemPrompt_forbids_pasting_links_in_the_body()
    {
        var prompt = GeneratorPromptBuilder.BuildSystemPrompt();

        prompt.Should().Contain("primer comentario");
        prompt.Should().Contain("Nunca pegues una URL");
    }

    [Fact]
    public void BuildSystemPrompt_caps_closing_emojis()
    {
        var prompt = GeneratorPromptBuilder.BuildSystemPrompt();

        prompt.Should().Contain("máximo 1 o 2 emojis");
    }

    [Fact]
    public void BuildSystemPrompt_requires_a_double_cta_open_to_feedback_and_opportunities()
    {
        var prompt = GeneratorPromptBuilder.BuildSystemPrompt();

        prompt.Should().Contain("feedback");
        prompt.Should().Contain("oportunidades laborales");
    }

    [Fact]
    public void BuildSystemPrompt_forbids_generic_filler_hashtags()
    {
        var prompt = GeneratorPromptBuilder.BuildSystemPrompt();

        prompt.Should().Contain("#OpenToWork");
        prompt.Should().Contain("genéricos de relleno");
    }

    [Fact]
    public void BuildSystemPrompt_only_allows_contexto_personal_when_present_in_the_payload()
    {
        var prompt = GeneratorPromptBuilder.BuildSystemPrompt();

        prompt.Should().Contain("contexto_personal");
        prompt.Should().Contain("no menciones situación laboral");
    }

    [Fact]
    public void BuildSystemPrompt_still_enforces_the_strict_JSON_output_contract()
    {
        var prompt = GeneratorPromptBuilder.BuildSystemPrompt();

        prompt.Should().Contain("hook, cuerpo, conclusion, cta, hashtags, fuentes");
        prompt.Should().Contain("1400 caracteres");
        prompt.Should().Contain("900");
    }

    [Fact]
    public void BuildPayload_includes_contexto_personal_when_configured()
    {
        var (contentIdea, activity, repository) = CreateGitHubFixture();

        var payload = GeneratorPromptBuilder.BuildPayload(
            contentIdea,
            [activity],
            [repository],
            null,
            ["sha-1"],
            [],
            "Sigo compaginando esto con mi trabajo actual.");

        payload.ContextoPersonal.Should().Be("Sigo compaginando esto con mi trabajo actual.");
    }

    [Fact]
    public void BuildPayload_leaves_contexto_personal_null_when_not_configured()
    {
        var (contentIdea, activity, repository) = CreateGitHubFixture();

        var payload = GeneratorPromptBuilder.BuildPayload(
            contentIdea,
            [activity],
            [repository],
            null,
            ["sha-1"],
            [],
            null);

        payload.ContextoPersonal.Should().BeNull();
    }

    private static (ContentIdea ContentIdea, GitHubActivity Activity, GitHubRepository Repository) CreateGitHubFixture()
    {
        var repository = new GitHubRepository(Guid.NewGuid(), "nleceguic", "dev-content-engine", Now);

        var activity = new GitHubActivity(
            Guid.NewGuid(),
            repository.Id,
            GitHubActivityType.Commit,
            ["C#", "PostgreSQL"],
            "Implement daily content pipeline",
            "sha-1",
            Now);

        var contentIdea = new ContentIdea(
            Guid.NewGuid(),
            ContentOrigin.GitHub,
            10m,
            [activity.Id],
            null,
            Now,
            ContentPath.GitHubPath);

        return (contentIdea, activity, repository);
    }
}
