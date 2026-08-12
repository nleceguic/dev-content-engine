using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;

namespace DevContentEngine.Application.Tests.TestHelpers;

internal static class GeneratedPostTestFactory
{
    public static GeneratedPost Create(GeneratedPostStatus status = GeneratedPostStatus.Draft, DateTime? now = null)
    {
        var timestamp = now ?? new DateTime(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

        var post = new GeneratedPost(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Hook",
            "Body",
            "Conclusion",
            "Cta",
            ["#dotnet"],
            ["https://github.com/owner/repo/commit/abc123"],
            "Dark navy background with glowing architecture nodes representing the pipeline.",
            ContentOrigin.GitHub,
            Guid.NewGuid(),
            timestamp);

        switch (status)
        {
            case GeneratedPostStatus.Edited:
                post.Edit("Hook", "Body", "Conclusion", "Cta", ["#dotnet"], timestamp);
                break;
            case GeneratedPostStatus.Used:
                post.MarkAsUsed(timestamp);
                break;
            case GeneratedPostStatus.Discarded:
                post.Discard(timestamp);
                break;
        }

        return post;
    }
}
