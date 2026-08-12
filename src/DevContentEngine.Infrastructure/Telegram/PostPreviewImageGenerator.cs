using System.Globalization;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DevContentEngine.Infrastructure.Telegram;

public sealed class PostPreviewImageGenerator
{
    private const int Width = 1200;
    private const int Height = 630;
    private const float Padding = 80f;

    private static readonly Color GitHubBackground = Color.ParseHex("1F6FEB");
    private static readonly Color TrendBackground = Color.ParseHex("D29922");
    private static readonly Color TextColor = Color.White;

    private static readonly string[] CandidateFontFamilies =
        ["Arial", "Liberation Sans", "DejaVu Sans", "Verdana", "Segoe UI", "Noto Sans", "Tahoma"];

    public MemoryStream Generate(string topic, string origin, string hook, DateTime date)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        ArgumentException.ThrowIfNullOrWhiteSpace(hook);

        var backgroundColor = origin.Contains("GitHub", StringComparison.OrdinalIgnoreCase)
            ? GitHubBackground
            : TrendBackground;

        using var image = new Image<Rgba32>(Width, Height);

        var titleFont = ResolveFont(56, FontStyle.Bold);
        var subtitleFont = ResolveFont(30, FontStyle.Regular);
        var badgeFont = ResolveFont(22, FontStyle.Regular);

        image.Mutate(context =>
        {
            context.Fill(backgroundColor);

            var textAreaWidth = Width - (Padding * 2);

            var titleOptions = new RichTextOptions(titleFont)
            {
                Origin = new PointF(Padding, 140),
                WrappingLength = textAreaWidth,
                LineSpacing = 1.15f
            };

            context.DrawText(titleOptions, topic, TextColor);

            var subtitleOptions = new RichTextOptions(subtitleFont)
            {
                Origin = new PointF(Padding, 320),
                WrappingLength = textAreaWidth,
                LineSpacing = 1.25f
            };

            context.DrawText(subtitleOptions, hook, TextColor);

            var badgeOptions = new RichTextOptions(badgeFont)
            {
                Origin = new PointF(Padding, Height - 80),
                WrappingLength = textAreaWidth
            };

            context.DrawText(badgeOptions, date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture), TextColor);
        });

        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;

        return stream;
    }

    private static Font ResolveFont(float size, FontStyle style)
    {
        foreach (var familyName in CandidateFontFamilies)
        {
            if (SystemFonts.Collection.TryGet(familyName, out var family))
            {
                return family.CreateFont(size, style);
            }
        }

        throw new InvalidOperationException(
            "No usable system font was found to render the post preview image. Install one of: " +
            string.Join(", ", CandidateFontFamilies));
    }
}
