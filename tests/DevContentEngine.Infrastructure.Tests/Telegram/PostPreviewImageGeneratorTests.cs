using DevContentEngine.Infrastructure.Telegram;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DevContentEngine.Infrastructure.Tests.Telegram;

public class PostPreviewImageGeneratorTests
{
    private static readonly DateTime Date = new(2026, 8, 10);
    private readonly PostPreviewImageGenerator _generator = new();

    [Fact]
    public void Generate_produces_a_valid_1200x630_PNG()
    {
        using var stream = _generator.Generate("Dev Content Engine", "GitHub/dev-content-engine", "Shipped the notifier today.", Date);

        stream.Length.Should().BeGreaterThan(0);

        var bytes = stream.ToArray();
        bytes.Take(8).Should().BeEquivalentTo(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "the stream must start with the PNG magic bytes");

        stream.Position = 0;
        using var image = Image.Load<Rgba32>(stream);

        image.Width.Should().Be(1200);
        image.Height.Should().Be(630);
    }

    [Fact]
    public void Generate_uses_a_blue_background_for_GitHub_origin()
    {
        using var stream = _generator.Generate("Topic", "GitHub/repo", "Hook", Date);
        stream.Position = 0;
        using var image = Image.Load<Rgba32>(stream);

        var corner = image[10, 10];

        corner.Should().Be(Color.ParseHex("1F6FEB").ToPixel<Rgba32>());
    }

    [Fact]
    public void Generate_uses_an_amber_background_for_Trend_origin()
    {
        using var stream = _generator.Generate("Topic", "Trend/dev.to", "Hook", Date);
        stream.Position = 0;
        using var image = Image.Load<Rgba32>(stream);

        var corner = image[10, 10];

        corner.Should().Be(Color.ParseHex("D29922").ToPixel<Rgba32>());
    }

    [Fact]
    public void Generate_does_not_throw_for_very_long_text()
    {
        var longTopic = string.Join(" ", Enumerable.Repeat("Palabra", 20));
        var longHook = string.Join(" ", Enumerable.Repeat("Una frase bastante larga sobre el pipeline", 15));

        var act = () => _generator.Generate(longTopic, "GitHub/repo", longHook, Date);

        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_does_not_throw_for_very_short_text()
    {
        var act = () => _generator.Generate("A", "GitHub/r", "B", Date);

        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_does_not_throw_for_special_characters_accents_and_emoji()
    {
        var act = () => _generator.Generate(
            "Depuración del pipeline 🚀",
            "GitHub/repo-ñandú",
            "Corregí una condición de carrera 🐛✅ en la validación — ¡funcionó a la primera!",
            Date);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("", "GitHub/repo", "Hook")]
    [InlineData("Topic", "", "Hook")]
    [InlineData("Topic", "GitHub/repo", "")]
    public void Generate_throws_for_missing_required_text(string topic, string origin, string hook)
    {
        var act = () => _generator.Generate(topic, origin, hook, Date);

        act.Should().Throw<ArgumentException>();
    }
}
