using DevContentEngine.Domain.Enums;

namespace DevContentEngine.Domain.Services;

public sealed record ActivityScoreResult(decimal Score, ContentPath ChosenPath);
