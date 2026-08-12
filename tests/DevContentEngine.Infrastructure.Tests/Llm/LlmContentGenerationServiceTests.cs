using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.External.Models;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Services;
using DevContentEngine.Infrastructure.Llm;
using DevContentEngine.Infrastructure.Llm.Prompts;
using DevContentEngine.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace DevContentEngine.Infrastructure.Tests.Llm;

public class LlmContentGenerationServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ILlmProvider> _llmProvider = new();
    private readonly Mock<IPromptVersionRepository> _promptVersionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    public LlmContentGenerationServiceTests()
    {
        _dateTimeProvider.Setup(provider => provider.UtcNow).Returns(Now);

        _promptVersionRepository
            .Setup(repository => repository.GetActiveAsync(It.IsAny<PromptRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PromptVersion?)null);

        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private LlmContentGenerationService CreateService(bool enableReviewer = false)
    {
        var options = Options.Create(new LlmOptions { EnableReviewer = enableReviewer });

        return new LlmContentGenerationService(
            _llmProvider.Object,
            _promptVersionRepository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object,
            new ContentValidator(),
            options,
            new InMemoryLogger<LlmContentGenerationService>());
    }

    private static ContentGenerationRequest CreateRequest()
    {
        var repository = new GitHubRepository(Guid.NewGuid(), "nleceguic", "dev-content-engine", Now);

        var activity = new GitHubActivity(
            Guid.NewGuid(),
            repository.Id,
            GitHubActivityType.Commit,
            ["C#", "PostgreSQL"],
            "Implement daily content pipeline",
            "sha-1,sha-2",
            Now);

        var contentIdea = new ContentIdea(
            Guid.NewGuid(),
            ContentOrigin.GitHub,
            10m,
            [activity.Id],
            null,
            Now,
            ContentPath.GitHubPath);

        return new ContentGenerationRequest(contentIdea, [activity], [repository], null, null, ["sha-1,sha-2"], [], Now);
    }

    private static GeneratorResponsePayload ValidDraft(string source = "sha-1,sha-2")
    {
        const string hook = "Terminé de conectar el pipeline diario con GitHub y Postgres.";
        const string conclusion = "Fue un buen ejercicio de diseño en capas y trazabilidad.";
        const string cta = "¿Qué opinas del enfoque?";
        var body = new string('a', Math.Max(1, 1000 - hook.Length - conclusion.Length - cta.Length));

        return new GeneratorResponsePayload(
            hook, body, conclusion, cta, ["#dotnet", "#postgres"], [source],
            "Dark navy background with glowing nodes for the daily content pipeline.");
    }

    private static GeneratorResponsePayload TooShortDraft() =>
        new(
            "Short hook.", "Short body.", "Short conclusion.", null, ["#dotnet"], ["sha-1"],
            "Dark navy background with glowing nodes.");

    private void SetupGenerator(params GeneratorResponsePayload[] responsesInOrder)
    {
        var callIndex = 0;

        _llmProvider
            .Setup(provider => provider.GenerateStructuredAsync<GeneratorResponsePayload>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<LlmGenerationOptions>(), It.IsAny<CancellationToken>()))
            .Returns<string, object, LlmGenerationOptions?, CancellationToken>((_, _, _, _) =>
                Task.FromResult(responsesInOrder[Math.Min(callIndex++, responsesInOrder.Length - 1)]));
    }

    private void SetupGeneratorCapturingSystemPrompt(List<string> capturedSystemPrompts, params GeneratorResponsePayload[] responsesInOrder)
    {
        var callIndex = 0;

        _llmProvider
            .Setup(provider => provider.GenerateStructuredAsync<GeneratorResponsePayload>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<LlmGenerationOptions>(), It.IsAny<CancellationToken>()))
            .Returns<string, object, LlmGenerationOptions?, CancellationToken>((systemPrompt, _, _, _) =>
            {
                capturedSystemPrompts.Add(systemPrompt);
                return Task.FromResult(responsesInOrder[Math.Min(callIndex++, responsesInOrder.Length - 1)]);
            });
    }

    private void SetupReviewer(params ReviewerResponsePayload[] responsesInOrder)
    {
        var callIndex = 0;

        _llmProvider
            .Setup(provider => provider.GenerateStructuredAsync<ReviewerResponsePayload>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<LlmGenerationOptions>(), It.IsAny<CancellationToken>()))
            .Returns<string, object, LlmGenerationOptions?, CancellationToken>((_, _, _, _) =>
                Task.FromResult(responsesInOrder[Math.Min(callIndex++, responsesInOrder.Length - 1)]));
    }

    [Fact]
    public async Task GenerateAsync_MVP_accepts_the_draft_as_soon_as_it_passes_deterministic_rules()
    {
        SetupGenerator(ValidDraft());

        var service = CreateService(enableReviewer: false);

        var result = await service.GenerateAsync(CreateRequest());

        result.Should().NotBeNull();
        result!.Hashtags.Should().BeEquivalentTo(["#dotnet", "#postgres"]);

        _llmProvider.Verify(
            provider => provider.GenerateStructuredAsync<GeneratorResponsePayload>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<LlmGenerationOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _llmProvider.Verify(
            provider => provider.GenerateStructuredAsync<ReviewerResponsePayload>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<LlmGenerationOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_builds_an_ImagePrompt_from_the_repository_technology_and_diagram_description()
    {
        SetupGenerator(ValidDraft());

        var service = CreateService(enableReviewer: false);

        var result = await service.GenerateAsync(CreateRequest());

        result.Should().NotBeNull();
        result!.ImagePrompt.Should().Contain("nleceguic/dev-content-engine");
        result.ImagePrompt.Should().Contain("C#");
        result.ImagePrompt.Should().Contain("Dark navy background with glowing nodes for the daily content pipeline.");
    }

    [Fact]
    public async Task GenerateAsync_returns_null_after_two_failed_deterministic_attempts()
    {
        SetupGenerator(TooShortDraft(), TooShortDraft());

        var service = CreateService(enableReviewer: false);

        var result = await service.GenerateAsync(CreateRequest());

        result.Should().BeNull();

        _llmProvider.Verify(
            provider => provider.GenerateStructuredAsync<GeneratorResponsePayload>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<LlmGenerationOptions>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateAsync_regenerates_with_a_corrective_system_prompt_and_succeeds_on_the_second_attempt()
    {
        var capturedSystemPrompts = new List<string>();
        SetupGeneratorCapturingSystemPrompt(capturedSystemPrompts, TooShortDraft(), ValidDraft());

        var service = CreateService(enableReviewer: false);

        var result = await service.GenerateAsync(CreateRequest());

        result.Should().NotBeNull();
        capturedSystemPrompts.Should().HaveCount(2);
        capturedSystemPrompts[0].Should().NotContain("ATENCIÓN");
        capturedSystemPrompts[1].Should().Contain("ATENCIÓN");
        capturedSystemPrompts[1].Should().Contain("expected between 700 and 1600");
    }

    [Fact]
    public async Task GenerateAsync_reviewer_enabled_accepts_when_both_deterministic_rules_and_reviewer_approve()
    {
        SetupGenerator(ValidDraft());
        SetupReviewer(new ReviewerResponsePayload("aprobado", null));

        var service = CreateService(enableReviewer: true);

        var result = await service.GenerateAsync(CreateRequest());

        result.Should().NotBeNull();

        _llmProvider.Verify(
            provider => provider.GenerateStructuredAsync<GeneratorResponsePayload>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<LlmGenerationOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _llmProvider.Verify(
            provider => provider.GenerateStructuredAsync<ReviewerResponsePayload>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<LlmGenerationOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_reviewer_enabled_regenerates_with_the_reviewers_notes_and_succeeds_on_retry()
    {
        var capturedSystemPrompts = new List<string>();
        SetupGeneratorCapturingSystemPrompt(capturedSystemPrompts, ValidDraft(), ValidDraft());
        SetupReviewer(
            new ReviewerResponsePayload("rechazado", "Menciona Kafka pero no aparece en los datos originales."),
            new ReviewerResponsePayload("aprobado", null));

        var service = CreateService(enableReviewer: true);

        var result = await service.GenerateAsync(CreateRequest());

        result.Should().NotBeNull();
        capturedSystemPrompts.Should().HaveCount(2);
        capturedSystemPrompts[1].Should().Contain("Menciona Kafka pero no aparece en los datos originales.");

        _llmProvider.Verify(
            provider => provider.GenerateStructuredAsync<ReviewerResponsePayload>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<LlmGenerationOptions>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateAsync_reviewer_enabled_returns_null_when_rejected_twice()
    {
        SetupGenerator(ValidDraft(), ValidDraft());
        SetupReviewer(
            new ReviewerResponsePayload("rechazado", "Motivo 1"),
            new ReviewerResponsePayload("rechazado", "Motivo 2"));

        var service = CreateService(enableReviewer: true);

        var result = await service.GenerateAsync(CreateRequest());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GenerateAsync_creates_and_persists_a_new_PromptVersion_when_none_is_active()
    {
        SetupGenerator(ValidDraft());

        PromptVersion? capturedPromptVersion = null;
        _promptVersionRepository
            .Setup(repository => repository.AddAsync(It.IsAny<PromptVersion>(), It.IsAny<CancellationToken>()))
            .Callback<PromptVersion, CancellationToken>((promptVersion, _) => capturedPromptVersion = promptVersion)
            .Returns(Task.CompletedTask);

        var service = CreateService(enableReviewer: false);

        var result = await service.GenerateAsync(CreateRequest());

        capturedPromptVersion.Should().NotBeNull();
        capturedPromptVersion!.Role.Should().Be(PromptRole.Generator);
        capturedPromptVersion.Content.Should().Be(GeneratorPromptBuilder.BuildSystemPrompt());
        result!.PromptVersionId.Should().Be(capturedPromptVersion.Id);

        _promptVersionRepository.Verify(repository => repository.AddAsync(It.IsAny<PromptVersion>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GenerateAsync_reuses_the_active_PromptVersion_when_its_content_is_unchanged()
    {
        var existingPromptVersion = new PromptVersion(Guid.NewGuid(), PromptRole.Generator, GeneratorPromptBuilder.BuildSystemPrompt(), Now);

        _promptVersionRepository
            .Setup(repository => repository.GetActiveAsync(PromptRole.Generator, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPromptVersion);

        SetupGenerator(ValidDraft());

        var service = CreateService(enableReviewer: false);

        var result = await service.GenerateAsync(CreateRequest());

        result!.PromptVersionId.Should().Be(existingPromptVersion.Id);
        existingPromptVersion.IsActive.Should().BeTrue();

        _promptVersionRepository.Verify(
            repository => repository.AddAsync(It.Is<PromptVersion>(p => p.Role == PromptRole.Generator), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_deactivates_the_old_PromptVersion_and_creates_a_new_one_when_content_differs()
    {
        var outdatedPromptVersion = new PromptVersion(Guid.NewGuid(), PromptRole.Generator, "Reglas antiguas del generador.", Now);

        _promptVersionRepository
            .Setup(repository => repository.GetActiveAsync(PromptRole.Generator, It.IsAny<CancellationToken>()))
            .ReturnsAsync(outdatedPromptVersion);

        SetupGenerator(ValidDraft());

        var service = CreateService(enableReviewer: false);

        var result = await service.GenerateAsync(CreateRequest());

        outdatedPromptVersion.IsActive.Should().BeFalse();
        result!.PromptVersionId.Should().NotBe(outdatedPromptVersion.Id);

        _promptVersionRepository.Verify(
            repository => repository.AddAsync(It.Is<PromptVersion>(p => p.Role == PromptRole.Generator), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
