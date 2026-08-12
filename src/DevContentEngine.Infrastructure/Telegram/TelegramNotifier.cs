using System.Net.Http.Json;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.External.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevContentEngine.Infrastructure.Telegram;

public sealed class TelegramNotifier : INotifier
{
    private const int CaptionMaxLength = 1024;

    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly PostPreviewImageGenerator _imageGenerator;
    private readonly ILogger<TelegramNotifier> _logger;

    public TelegramNotifier(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        PostPreviewImageGenerator imageGenerator,
        ILogger<TelegramNotifier> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _imageGenerator = imageGenerator;
        _logger = logger;
    }

    public async Task NotifyDraftReadyAsync(DraftReadyNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var caption = BuildDraftReadyCaption(notification);

        using var imageStream = _imageGenerator.Generate(
            notification.Topic,
            notification.Origin,
            notification.Hook,
            notification.GeneratedAt);

        using var content = new MultipartFormDataContent
        {
            { new StringContent(_options.ChatId), "chat_id" },
            { new StringContent(caption), "caption" }
        };

        using var photoContent = new StreamContent(imageStream);
        photoContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(photoContent, "photo", $"post-preview-{notification.GeneratedPostId}.png");

        await SendAsync("sendPhoto", content, cancellationToken);
    }

    public async Task NotifyPipelineFailedAsync(string reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        await SendTextAsync($"⚠️ No se pudo generar el post de hoy: {reason}", cancellationToken);
    }

    public async Task NotifyNoContentApprovedAsync(string reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        await SendTextAsync($"⚠️ No se aprobó ningún borrador tras los reintentos: {reason}", cancellationToken);
    }

    private async Task SendTextAsync(string text, CancellationToken cancellationToken)
    {
        var payload = new { chat_id = _options.ChatId, text };

        using var content = JsonContent.Create(payload);

        await SendAsync("sendMessage", content, cancellationToken);
    }

    private async Task SendAsync(string method, HttpContent content, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;

        var requestUri = new Uri($"./bot{_options.BotToken}/{method}", UriKind.Relative);

        try
        {
            response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            throw new TelegramNotificationException($"Failed to reach the Telegram Bot API ({method}): {ex.Message}", ex);
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await SafeReadBodyAsync(response, cancellationToken);

            throw new TelegramNotificationException(
                $"Telegram Bot API ({method}) returned {(int)response.StatusCode} ({response.StatusCode}): {body}");
        }
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception)
        {
            return "<no response body>";
        }
    }

    internal static string BuildDraftReadyCaption(DraftReadyNotification notification)
    {
        var header =
            $"📝 Post preparado — {notification.GeneratedAt:HH:mm}\n\n" +
            $"Tema: {notification.Topic}\n" +
            $"Origen: {notification.Origin}\n" +
            $"Motivo: {notification.Reason}\n\n";

        var rest = string.Join(
            "\n\n",
            new[] { notification.Body, notification.Conclusion, notification.Cta }.Where(text => !string.IsNullOrWhiteSpace(text)));

        var prefix = $"{header}{notification.Hook}\n\n";
        var fullCaption = $"{prefix}{rest}";

        if (fullCaption.Length <= CaptionMaxLength)
        {
            return fullCaption;
        }

        var notice = $"\n\n… borrador completo disponible en /drafts/{notification.GeneratedPostId}";
        var availableForRest = CaptionMaxLength - prefix.Length - notice.Length;

        if (availableForRest > 0)
        {
            var truncatedRest = rest.Length > availableForRest ? rest[..availableForRest] : rest;
            return $"{prefix}{truncatedRest}{notice}";
        }

        var hardLimit = Math.Max(0, CaptionMaxLength - notice.Length);
        var truncatedFull = fullCaption.Length > hardLimit ? fullCaption[..hardLimit] : fullCaption;

        return $"{truncatedFull}{notice}";
    }
}
