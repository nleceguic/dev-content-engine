namespace DevContentEngine.Application.Interfaces.External.Models;

public sealed record LlmGenerationOptions(double? Temperature = null, int? MaxOutputTokens = null);
