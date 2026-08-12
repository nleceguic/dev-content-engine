namespace DevContentEngine.Infrastructure.Telegram;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; set; } = string.Empty;

    public string ChatId { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = "https://api.telegram.org/";
}
