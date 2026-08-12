namespace DevContentEngine.Infrastructure.Telegram;

public sealed class TelegramNotificationException : Exception
{
    public TelegramNotificationException(string message)
        : base(message)
    {
    }

    public TelegramNotificationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
