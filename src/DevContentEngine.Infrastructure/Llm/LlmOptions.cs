namespace DevContentEngine.Infrastructure.Llm;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "claude-sonnet-4-5-20250929";

    public string ApiUrl { get; set; } = "https://api.anthropic.com/";

    public string AnthropicVersion { get; set; } = "2023-06-01";

    public int MaxOutputTokens { get; set; } = 1024;

    public bool EnableReviewer { get; set; } = false;
}
