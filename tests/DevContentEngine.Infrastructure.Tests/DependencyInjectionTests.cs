using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Services;
using DevContentEngine.Infrastructure.Persistence;
using DevContentEngine.Infrastructure.Telegram;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DevContentEngine.Infrastructure.Tests;

public class DependencyInjectionTests
{
    private static ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=devcontentengine;Username=postgres;Password=postgres",
                ["GitHub:Token"] = "fine-grained-test-token",
                ["Llm:ApiKey"] = "test-api-key",
                ["Telegram:BotToken"] = "test-bot-token",
                ["Telegram:ChatId"] = "12345"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddInfrastructure_registers_the_DbContext_and_every_repository()
    {
        using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<DevContentEngineDbContext>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IGitHubRepositoryRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IGitHubActivityRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IContentIdeaRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IGeneratedPostRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IGenerationRunRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IPromptVersionRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ITrendRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IDateTimeProvider>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IGitHubClient>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ITrendSource>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ILlmProvider>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ContentValidator>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ActivityNoiseFilter>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<TechnologyExtractor>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ActivityScorer>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<TopicRepetitionDetector>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IContentGenerationService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<PostPreviewImageGenerator>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<INotifier>().Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructureHealthChecks_registers_the_postgresql_github_and_llm_checks()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=devcontentengine;Username=postgres;Password=postgres",
                ["GitHub:Token"] = "fine-grained-test-token",
                ["Llm:ApiKey"] = "test-api-key",
                ["Telegram:BotToken"] = "test-bot-token",
                ["Telegram:ChatId"] = "12345"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        services.AddInfrastructureHealthChecks();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var registrations = scope.ServiceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        registrations.Select(registration => registration.Name).Should().BeEquivalentTo("postgresql", "github", "llm");
    }

    [Fact]
    public void AddInfrastructure_throws_when_the_connection_string_is_missing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        var act = () => services.AddInfrastructure(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Resolving_IGitHubClient_throws_when_the_GitHub_token_is_missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=devcontentengine;Username=postgres;Password=postgres"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var act = () => scope.ServiceProvider.GetRequiredService<IGitHubClient>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Resolving_ILlmProvider_throws_when_the_Llm_api_key_is_missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=devcontentengine;Username=postgres;Password=postgres"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var act = () => scope.ServiceProvider.GetRequiredService<ILlmProvider>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Resolving_INotifier_throws_when_the_Telegram_bot_token_is_missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=devcontentengine;Username=postgres;Password=postgres",
                ["Telegram:ChatId"] = "12345"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var act = () => scope.ServiceProvider.GetRequiredService<INotifier>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Resolving_INotifier_throws_when_the_Telegram_chat_id_is_missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=devcontentengine;Username=postgres;Password=postgres",
                ["Telegram:BotToken"] = "test-bot-token"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var act = () => scope.ServiceProvider.GetRequiredService<INotifier>();

        act.Should().Throw<InvalidOperationException>();
    }
}
