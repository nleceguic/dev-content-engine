using System.Net;
using System.Text;
using System.Text.Json;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Infrastructure.Llm;
using DevContentEngine.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevContentEngine.Infrastructure.Tests.Llm;

public class AnthropicLlmProviderTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private sealed record TestDto(string Hook, string Body);

    private static AnthropicLlmProvider CreateProvider(FakeHttpMessageHandler handler, InMemoryLogger<AnthropicLlmProvider>? logger = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com/") };
        var options = Options.Create(new LlmOptions { ApiKey = "test-key", Model = "claude-test" });

        return new AnthropicLlmProvider(httpClient, options, logger ?? new InMemoryLogger<AnthropicLlmProvider>());
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string body) =>
        new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static string BuildAnthropicResponseBody(string text)
    {
        var response = new
        {
            id = "msg_test",
            type = "message",
            role = "assistant",
            content = new[] { new { type = "text", text } },
            model = "claude-test",
            stop_reason = "end_turn"
        };

        return JsonSerializer.Serialize(response, SerializerOptions);
    }

    [Fact]
    public async Task GenerateStructuredAsync_returns_the_parsed_result_when_the_first_response_is_valid_JSON()
    {
        var expected = new TestDto("Ship the pipeline", "We shipped the daily content pipeline this week.");
        var validJson = JsonSerializer.Serialize(expected, SerializerOptions);

        var handler = new FakeHttpMessageHandler(
            () => JsonResponse(HttpStatusCode.OK, BuildAnthropicResponseBody(validJson)));

        var provider = CreateProvider(handler);

        var result = await provider.GenerateStructuredAsync<TestDto>("Generate a LinkedIn post.", new { activity = "commits" });

        result.Should().BeEquivalentTo(expected);
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GenerateStructuredAsync_parses_the_result_when_wrapped_in_a_markdown_code_fence()
    {
        var expected = new TestDto("Ship the pipeline", "We shipped the daily content pipeline this week.");
        var validJson = JsonSerializer.Serialize(expected, SerializerOptions);
        var fencedJson = $"```json\n{validJson}\n```";

        var handler = new FakeHttpMessageHandler(
            () => JsonResponse(HttpStatusCode.OK, BuildAnthropicResponseBody(fencedJson)));

        var provider = CreateProvider(handler);

        var result = await provider.GenerateStructuredAsync<TestDto>("Generate a LinkedIn post.", new { activity = "commits" });

        result.Should().BeEquivalentTo(expected);
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GenerateStructuredAsync_retries_once_and_succeeds_when_the_first_response_is_malformed()
    {
        var expected = new TestDto("Ship the pipeline", "We shipped the daily content pipeline this week.");
        var validJson = JsonSerializer.Serialize(expected, SerializerOptions);
        var malformedText = "Sure! Here's the JSON you asked for:\n" + validJson;

        var handler = new FakeHttpMessageHandler(
            () => JsonResponse(HttpStatusCode.OK, BuildAnthropicResponseBody(malformedText)),
            () => JsonResponse(HttpStatusCode.OK, BuildAnthropicResponseBody(validJson)));

        var logger = new InMemoryLogger<AnthropicLlmProvider>();
        var provider = CreateProvider(handler, logger);

        var result = await provider.GenerateStructuredAsync<TestDto>("Generate a LinkedIn post.", new { activity = "commits" });

        result.Should().BeEquivalentTo(expected);
        handler.CallCount.Should().Be(2);
        logger.Entries.Should().Contain(entry => entry.Level == LogLevel.Warning);
        handler.RequestBodies[1].Should().Contain("Your previous response was not valid JSON");
    }

    [Fact]
    public async Task GenerateStructuredAsync_throws_LlmGenerationException_when_both_attempts_return_malformed_JSON()
    {
        var handler = new FakeHttpMessageHandler(
            () => JsonResponse(HttpStatusCode.OK, BuildAnthropicResponseBody("not json at all")),
            () => JsonResponse(HttpStatusCode.OK, BuildAnthropicResponseBody("still not json")));

        var provider = CreateProvider(handler);

        var act = async () => await provider.GenerateStructuredAsync<TestDto>("Generate a LinkedIn post.", new { activity = "commits" });

        await act.Should().ThrowAsync<LlmGenerationException>();
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task GenerateStructuredAsync_throws_LlmGenerationException_on_an_HTTP_failure_without_retrying()
    {
        var handler = new FakeHttpMessageHandler(
            () => new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"error":{"type":"authentication_error","message":"invalid x-api-key"}}""")
            });

        var provider = CreateProvider(handler);

        var act = async () => await provider.GenerateStructuredAsync<TestDto>("Generate a LinkedIn post.", new { activity = "commits" });

        var exception = await act.Should().ThrowAsync<LlmGenerationException>();
        exception.Which.Message.Should().Contain("401");
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GenerateStructuredAsync_sends_max_tokens_in_snake_case_as_required_by_the_Anthropic_API()
    {
        var handler = new FakeHttpMessageHandler(
            () => JsonResponse(HttpStatusCode.OK, BuildAnthropicResponseBody("""{"Hook":"h","Body":"b"}""")));

        var provider = CreateProvider(handler);

        await provider.GenerateStructuredAsync<TestDto>("Generate a LinkedIn post.", new { activity = "commits" });

        handler.RequestBodies[0].Should().Contain("\"max_tokens\"");
        handler.RequestBodies[0].Should().NotContain("\"maxTokens\"");
    }

    [Fact]
    public async Task GenerateAsync_omits_system_and_temperature_when_unset_instead_of_sending_JSON_null()
    {
        var handler = new FakeHttpMessageHandler(
            () => JsonResponse(HttpStatusCode.OK, BuildAnthropicResponseBody("Hello from Claude")));

        var provider = CreateProvider(handler);

        await provider.GenerateAsync("Say hello");

        handler.RequestBodies[0].Should().NotContain("\"system\"");
        handler.RequestBodies[0].Should().NotContain("\"temperature\"");
    }

    [Fact]
    public async Task GenerateAsync_returns_the_models_free_text_response()
    {
        var handler = new FakeHttpMessageHandler(
            () => JsonResponse(HttpStatusCode.OK, BuildAnthropicResponseBody("Hello from Claude")));

        var provider = CreateProvider(handler);

        var result = await provider.GenerateAsync("Say hello");

        result.Should().Be("Hello from Claude");
    }
}
