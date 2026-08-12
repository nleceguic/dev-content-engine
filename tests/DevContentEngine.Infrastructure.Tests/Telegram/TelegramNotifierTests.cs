using System.Net;
using System.Text.Json;
using DevContentEngine.Application.Interfaces.External.Models;
using DevContentEngine.Infrastructure.Telegram;
using DevContentEngine.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace DevContentEngine.Infrastructure.Tests.Telegram;

public class TelegramNotifierTests
{
    private static readonly DateTime GeneratedAt = new(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

    private static TelegramNotifier CreateNotifier(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org/") };
        var options = Options.Create(new TelegramOptions { BotToken = "test-token", ChatId = "12345" });

        return new TelegramNotifier(httpClient, options, new PostPreviewImageGenerator(), new InMemoryLogger<TelegramNotifier>());
    }

    private static HttpResponseMessage OkResponse() =>
        new(HttpStatusCode.OK) { Content = new StringContent("""{"ok":true}""") };

    private static string ExtractJsonTextField(string json) =>
        JsonDocument.Parse(json).RootElement.GetProperty("text").GetString()!;

    private static DraftReadyNotification CreateNotification(
        Guid? id = null,
        string body = "Cuerpo del post con el detalle de lo que hice.",
        string? conclusion = "Fue un buen ejercicio de diseño en capas.",
        string? cta = "¿Qué opinas?") =>
        new(
            id ?? Guid.NewGuid(),
            "Pipeline de contenido diario",
            "GitHub/dev-content-engine",
            "Score de actividad alto por varios commits relevantes.",
            "Terminé de conectar el pipeline con Telegram.",
            body,
            conclusion ?? string.Empty,
            cta,
            GeneratedAt);

    [Fact]
    public async Task NotifyPipelineFailedAsync_resolves_against_BaseAddress_even_when_the_bot_token_contains_a_colon()
    {
        var handler = new FakeHttpMessageHandler(OkResponse);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org/") };
        var options = Options.Create(new TelegramOptions { BotToken = "123456:AAEtestSecretValue", ChatId = "12345" });
        var notifier = new TelegramNotifier(httpClient, options, new PostPreviewImageGenerator(), new InMemoryLogger<TelegramNotifier>());

        await notifier.NotifyPipelineFailedAsync("GitHub API is unavailable");

        handler.RequestUris[0]!.Host.Should().Be("api.telegram.org");
        handler.RequestUris[0]!.AbsoluteUri.Should().Be("https://api.telegram.org/bot123456:AAEtestSecretValue/sendMessage");
    }

    [Fact]
    public async Task NotifyPipelineFailedAsync_sends_the_exact_message_via_sendMessage()
    {
        var handler = new FakeHttpMessageHandler(OkResponse);
        var notifier = CreateNotifier(handler);

        await notifier.NotifyPipelineFailedAsync("GitHub API is unavailable");

        handler.CallCount.Should().Be(1);
        handler.RequestUris[0]!.ToString().Should().EndWith("bottest-token/sendMessage");
        handler.ContentTypes[0].Should().Be("application/json");
        ExtractJsonTextField(handler.RequestBodies[0]).Should().Be("⚠️ No se pudo generar el post de hoy: GitHub API is unavailable");
        handler.RequestBodies[0].Should().Contain("\"chat_id\":\"12345\"");
    }

    [Fact]
    public async Task NotifyNoContentApprovedAsync_sends_the_exact_message_via_sendMessage()
    {
        var handler = new FakeHttpMessageHandler(OkResponse);
        var notifier = CreateNotifier(handler);

        await notifier.NotifyNoContentApprovedAsync("El revisor rechazó los dos intentos");

        handler.CallCount.Should().Be(1);
        handler.RequestUris[0]!.ToString().Should().EndWith("bottest-token/sendMessage");
        handler.ContentTypes[0].Should().Be("application/json");
        ExtractJsonTextField(handler.RequestBodies[0]).Should().Be("⚠️ No se aprobó ningún borrador tras los reintentos: El revisor rechazó los dos intentos");
    }

    [Fact]
    public async Task NotifyDraftReadyAsync_calls_sendPhoto_not_sendMessage_with_a_multipart_image_attachment()
    {
        var handler = new FakeHttpMessageHandler(OkResponse);
        var notifier = CreateNotifier(handler);
        var postId = Guid.NewGuid();

        await notifier.NotifyDraftReadyAsync(CreateNotification(postId));

        handler.CallCount.Should().Be(1);
        handler.RequestUris[0]!.ToString().Should().EndWith("bottest-token/sendPhoto");
        handler.RequestUris[0]!.ToString().Should().NotContain("sendMessage");
        handler.ContentTypes[0].Should().Be("multipart/form-data");

        var body = handler.RequestBodies[0];
        body.Should().Contain("name=chat_id");
        body.Should().Contain("name=caption");
        body.Should().Contain($"name=photo; filename=post-preview-{postId}.png");
        body.Should().Contain("Content-Type: image/png");
        body.Should().Contain("PNG");
    }

    [Fact]
    public async Task NotifyDraftReadyAsync_caption_follows_the_blueprint_format()
    {
        var handler = new FakeHttpMessageHandler(OkResponse);
        var notifier = CreateNotifier(handler);

        await notifier.NotifyDraftReadyAsync(CreateNotification());

        var body = handler.RequestBodies[0];

        body.Should().Contain("📝 Post preparado — 08:00");
        body.Should().Contain("Tema: Pipeline de contenido diario");
        body.Should().Contain("Origen: GitHub/dev-content-engine");
        body.Should().Contain("Motivo: Score de actividad alto por varios commits relevantes.");
        body.Should().Contain("Terminé de conectar el pipeline con Telegram.");
        body.Should().Contain("Cuerpo del post con el detalle de lo que hice.");
    }

    [Fact]
    public void BuildDraftReadyCaption_matches_the_exact_blueprint_format()
    {
        var notification = CreateNotification();

        var caption = TelegramNotifier.BuildDraftReadyCaption(notification);

        var expected =
            "📝 Post preparado — 08:00\n\n" +
            "Tema: Pipeline de contenido diario\n" +
            "Origen: GitHub/dev-content-engine\n" +
            "Motivo: Score de actividad alto por varios commits relevantes.\n\n" +
            "Terminé de conectar el pipeline con Telegram.\n\n" +
            "Cuerpo del post con el detalle de lo que hice.\n\n" +
            "Fue un buen ejercicio de diseño en capas.\n\n" +
            "¿Qué opinas?";

        caption.Should().Be(expected);
    }

    [Fact]
    public void BuildDraftReadyCaption_omits_a_null_or_empty_cta()
    {
        var notification = CreateNotification(cta: null);

        var caption = TelegramNotifier.BuildDraftReadyCaption(notification);

        caption.Should().NotContain("¿Qué opinas?");
        caption.Should().EndWith("Fue un buen ejercicio de diseño en capas.");
    }

    [Fact]
    public void BuildDraftReadyCaption_truncates_and_points_to_the_drafts_link_when_the_post_does_not_fit()
    {
        var postId = Guid.NewGuid();
        var notification = CreateNotification(postId, body: new string('a', 2000));

        var caption = TelegramNotifier.BuildDraftReadyCaption(notification);

        caption.Length.Should().BeLessThanOrEqualTo(1024);
        caption.Should().Contain($"borrador completo disponible en /drafts/{postId}");
        caption.Should().StartWith("📝 Post preparado — 08:00");
        caption.Should().Contain("Terminé de conectar el pipeline con Telegram.");
    }

    [Fact]
    public void BuildDraftReadyCaption_never_exceeds_the_Telegram_caption_limit_even_with_an_extremely_long_hook()
    {
        var notification = CreateNotification() with { Hook = new string('h', 5000) };

        var caption = TelegramNotifier.BuildDraftReadyCaption(notification);

        caption.Length.Should().BeLessThanOrEqualTo(1024);
    }
}
