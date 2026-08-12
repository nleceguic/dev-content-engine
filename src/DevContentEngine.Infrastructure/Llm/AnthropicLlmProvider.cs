using System.Net.Http.Json;
using System.Text.Json;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.External.Models;
using DevContentEngine.Infrastructure.Llm.Anthropic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevContentEngine.Infrastructure.Llm;

public sealed class AnthropicLlmProvider : ILlmProvider
{
    private const string JsonInstruction =
        "Respond with valid JSON only. Do not include any explanatory text, markdown code fences, or commentary outside the JSON object.";

    private const string RetryInstruction =
        "Your previous response was not valid JSON. Respond only with valid JSON, with no additional text.";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly LlmOptions _options;
    private readonly ILogger<AnthropicLlmProvider> _logger;

    public AnthropicLlmProvider(HttpClient httpClient, IOptions<LlmOptions> options, ILogger<AnthropicLlmProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(
        string prompt,
        LlmGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var messages = new List<AnthropicMessage> { new("user", prompt) };
        var response = await SendMessageRequestAsync(systemPrompt: null, messages, options, cancellationToken);

        return ExtractText(response);
    }

    public async Task<T> GenerateStructuredAsync<T>(
        string systemPrompt,
        object userPayload,
        LlmGenerationOptions? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);
        ArgumentNullException.ThrowIfNull(userPayload);

        var effectiveSystemPrompt = $"{systemPrompt}\n\n{JsonInstruction}";
        var userContent = JsonSerializer.Serialize(userPayload, SerializerOptions);
        var messages = new List<AnthropicMessage> { new("user", userContent) };

        var firstResponse = await SendMessageRequestAsync(effectiveSystemPrompt, messages, options, cancellationToken);
        var firstText = ExtractText(firstResponse);
        var firstResult = TryParse<T>(firstText);

        if (firstResult is not null)
        {
            return firstResult;
        }

        _logger.LogWarning(
            "LLM response for {Type} was not valid JSON on the first attempt; retrying with a corrective message.",
            typeof(T).Name);

        messages.Add(new AnthropicMessage("assistant", firstText));
        messages.Add(new AnthropicMessage("user", RetryInstruction));

        var retryResponse = await SendMessageRequestAsync(effectiveSystemPrompt, messages, options, cancellationToken);
        var retryText = ExtractText(retryResponse);
        var retryResult = TryParse<T>(retryText);

        return retryResult ?? throw new LlmGenerationException(
            $"The model did not return valid JSON for {typeof(T).Name} after 2 attempts. Last response: {retryText}");
    }

    private async Task<AnthropicMessageResponse> SendMessageRequestAsync(
        string? systemPrompt,
        IReadOnlyCollection<AnthropicMessage> messages,
        LlmGenerationOptions? options,
        CancellationToken cancellationToken)
    {
        var requestBody = new AnthropicMessageRequest(
            _options.Model,
            options?.MaxOutputTokens ?? _options.MaxOutputTokens,
            systemPrompt,
            options?.Temperature,
            messages);

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync("v1/messages", requestBody, SerializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            throw new LlmGenerationException($"Failed to reach the LLM API: {ex.Message}", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await SafeReadBodyAsync(response, cancellationToken);
                throw new LlmGenerationException($"LLM API returned {(int)response.StatusCode} ({response.StatusCode}): {body}");
            }

            AnthropicMessageResponse? payload;

            try
            {
                payload = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>(SerializerOptions, cancellationToken);
            }
            catch (JsonException ex)
            {
                throw new LlmGenerationException("LLM API returned a response that could not be parsed.", ex);
            }

            return payload ?? throw new LlmGenerationException("LLM API returned an empty response.");
        }
    }

    private static string ExtractText(AnthropicMessageResponse response)
    {
        var text = response.Content?.FirstOrDefault(block => block.Type == "text")?.Text;

        return string.IsNullOrWhiteSpace(text)
            ? throw new LlmGenerationException("LLM API response did not include any text content.")
            : text;
    }

    private static T? TryParse<T>(string text) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(StripMarkdownCodeFence(text), SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string StripMarkdownCodeFence(string text)
    {
        var trimmed = text.Trim();

        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return text;
        }

        var firstNewline = trimmed.IndexOf('\n');

        if (firstNewline < 0)
        {
            return text;
        }

        var withoutOpeningFence = trimmed[(firstNewline + 1)..];
        var closingFenceIndex = withoutOpeningFence.LastIndexOf("```", StringComparison.Ordinal);

        return closingFenceIndex < 0 ? text : withoutOpeningFence[..closingFenceIndex].Trim();
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
}
