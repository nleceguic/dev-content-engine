using System.Text.Json.Serialization;

namespace DevContentEngine.Infrastructure.Llm.Anthropic;

internal sealed record AnthropicMessageRequest(
    string Model,
    [property: JsonPropertyName("max_tokens")] int MaxTokens,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? System,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Temperature,
    IReadOnlyCollection<AnthropicMessage> Messages);

internal sealed record AnthropicMessage(string Role, string Content);

internal sealed record AnthropicMessageResponse(IReadOnlyList<AnthropicContentBlock>? Content);

internal sealed record AnthropicContentBlock(string Type, string? Text);
