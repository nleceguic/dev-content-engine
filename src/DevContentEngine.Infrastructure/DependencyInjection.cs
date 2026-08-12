using System.Net.Http.Headers;
using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Services;
using DevContentEngine.Infrastructure.Common;
using DevContentEngine.Infrastructure.GitHub;
using DevContentEngine.Infrastructure.HealthChecks;
using DevContentEngine.Infrastructure.Llm;
using DevContentEngine.Infrastructure.Persistence;
using DevContentEngine.Infrastructure.Persistence.Repositories;
using DevContentEngine.Infrastructure.Telegram;
using DevContentEngine.Infrastructure.Trends;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DevContentEngine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'ConnectionStrings:Default' is not configured. Set the ConnectionStrings__Default environment variable.");

        services.AddDbContext<DevContentEngineDbContext>(options => options.UseNpgsql(connectionString));

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IGitHubRepositoryRepository, GitHubRepositoryRepository>();
        services.AddScoped<IGitHubActivityRepository, GitHubActivityRepository>();
        services.AddScoped<IContentIdeaRepository, ContentIdeaRepository>();
        services.AddScoped<IGeneratedPostRepository, GeneratedPostRepository>();
        services.AddScoped<IGenerationRunRepository, GenerationRunRepository>();
        services.AddScoped<IPromptVersionRepository, PromptVersionRepository>();
        services.AddScoped<ITrendRepository, TrendRepository>();

        services.Configure<GitHubOptions>(configuration.GetSection(GitHubOptions.SectionName));

        services.AddHttpClient<IGitHubClient, GitHubClient>((serviceProvider, client) =>
        {
            var gitHubOptions = serviceProvider.GetRequiredService<IOptions<GitHubOptions>>().Value;

            if (string.IsNullOrWhiteSpace(gitHubOptions.Token))
            {
                throw new InvalidOperationException(
                    "GitHub personal access token is not configured. Set the GitHub__Token environment variable.");
            }

            client.BaseAddress = new Uri(gitHubOptions.ApiUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gitHubOptions.Token);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DevContentEngine", "1.0"));
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));

        services.AddHttpClient<ILlmProvider, AnthropicLlmProvider>((serviceProvider, client) =>
        {
            var llmOptions = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;

            if (string.IsNullOrWhiteSpace(llmOptions.ApiKey))
            {
                throw new InvalidOperationException(
                    "LLM API key is not configured. Set the Llm__ApiKey environment variable.");
            }

            client.BaseAddress = new Uri(llmOptions.ApiUrl);
            client.DefaultRequestHeaders.Add("x-api-key", llmOptions.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", llmOptions.AnthropicVersion);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddSingleton(_ => new ContentValidator());
        services.AddSingleton(_ => new ActivityNoiseFilter());
        services.AddSingleton(_ => new TechnologyExtractor());
        services.AddSingleton<ActivityScorer>();
        services.AddSingleton<TopicRepetitionDetector>();
        services.AddScoped<IContentGenerationService, LlmContentGenerationService>();

        services.AddScoped<ITrendSource, NullTrendSource>();

        services.Configure<TelegramOptions>(configuration.GetSection(TelegramOptions.SectionName));
        services.AddSingleton<PostPreviewImageGenerator>();

        services.AddHttpClient<INotifier, TelegramNotifier>((serviceProvider, client) =>
        {
            var telegramOptions = serviceProvider.GetRequiredService<IOptions<TelegramOptions>>().Value;

            if (string.IsNullOrWhiteSpace(telegramOptions.BotToken))
            {
                throw new InvalidOperationException(
                    "Telegram bot token is not configured. Set the Telegram__BotToken environment variable.");
            }

            if (string.IsNullOrWhiteSpace(telegramOptions.ChatId))
            {
                throw new InvalidOperationException(
                    "Telegram chat id is not configured. Set the Telegram__ChatId environment variable.");
            }

            client.BaseAddress = new Uri(telegramOptions.ApiUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static IServiceCollection AddInfrastructureHealthChecks(this IServiceCollection services)
    {
        services.AddHttpClient<GitHubApiHealthCheck>((serviceProvider, client) =>
        {
            var gitHubOptions = serviceProvider.GetRequiredService<IOptions<GitHubOptions>>().Value;

            client.BaseAddress = new Uri(gitHubOptions.ApiUrl);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DevContentEngine", "1.0"));
            client.Timeout = TimeSpan.FromSeconds(10);

            if (!string.IsNullOrWhiteSpace(gitHubOptions.Token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gitHubOptions.Token);
            }
        });

        services.AddHttpClient<LlmProviderHealthCheck>((serviceProvider, client) =>
        {
            var llmOptions = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;

            client.BaseAddress = new Uri(llmOptions.ApiUrl);
            client.Timeout = TimeSpan.FromSeconds(10);

            if (!string.IsNullOrWhiteSpace(llmOptions.ApiKey))
            {
                client.DefaultRequestHeaders.Add("x-api-key", llmOptions.ApiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", llmOptions.AnthropicVersion);
            }
        });

        services.AddHealthChecks()
            .AddCheck<PostgreSqlHealthCheck>("postgresql")
            .AddCheck<GitHubApiHealthCheck>("github")
            .AddCheck<LlmProviderHealthCheck>("llm");

        return services;
    }
}
