using DevContentEngine.Application.Common;
using DevContentEngine.Application.Interfaces.External;
using DevContentEngine.Application.Interfaces.External.Models;
using DevContentEngine.Application.Interfaces.Persistence;
using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Services;
using DevContentEngine.Infrastructure.Llm.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevContentEngine.Infrastructure.Llm;

public sealed class LlmContentGenerationService : IContentGenerationService
{
    private const int MaxAttempts = 2;

    private readonly ILlmProvider _llmProvider;
    private readonly IPromptVersionRepository _promptVersionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ContentValidator _contentValidator;
    private readonly LlmOptions _options;
    private readonly ILogger<LlmContentGenerationService> _logger;

    public LlmContentGenerationService(
        ILlmProvider llmProvider,
        IPromptVersionRepository promptVersionRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ContentValidator contentValidator,
        IOptions<LlmOptions> options,
        ILogger<LlmContentGenerationService> logger)
    {
        _llmProvider = llmProvider;
        _promptVersionRepository = promptVersionRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _contentValidator = contentValidator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GeneratedContentResult?> GenerateAsync(
        ContentGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var generatorSystemPrompt = GeneratorPromptBuilder.BuildSystemPrompt();
        var generatorPromptVersion = await ResolveOrCreatePromptVersionAsync(
            PromptRole.Generator,
            generatorSystemPrompt,
            cancellationToken);

        var payload = GeneratorPromptBuilder.BuildPayload(
            request.ContentIdea,
            request.Activities,
            request.Repositories,
            request.Trend,
            request.SourceReferences,
            request.RecentPosts);

        string? correctionNotes = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var (result, rejectionReason) = await TryGenerateOnceAsync(
                generatorSystemPrompt,
                payload,
                correctionNotes,
                generatorPromptVersion.Id,
                request,
                cancellationToken);

            if (result is not null)
            {
                return result;
            }

            if (attempt == MaxAttempts)
            {
                _logger.LogWarning(
                    "Giving up on content generation after {Attempts} attempts. Last rejection reason: {Reason}",
                    MaxAttempts,
                    rejectionReason);

                return null;
            }

            correctionNotes = rejectionReason;
        }

        return null;
    }

    private async Task<(GeneratedContentResult? Result, string? RejectionReason)> TryGenerateOnceAsync(
        string generatorSystemPrompt,
        GeneratorPayload payload,
        string? correctionNotes,
        Guid generatorPromptVersionId,
        ContentGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var effectiveSystemPrompt = correctionNotes is null
            ? generatorSystemPrompt
            : $"{generatorSystemPrompt}\n\n{BuildCorrectionAddendum(correctionNotes)}";

        var draft = await _llmProvider.GenerateStructuredAsync<GeneratorResponsePayload>(
            effectiveSystemPrompt,
            payload,
            cancellationToken: cancellationToken);

        var draftContent = new DraftContent(draft.Hook, draft.Cuerpo, draft.Conclusion, draft.Cta, draft.Hashtags, draft.Fuentes);
        var validationResult = _contentValidator.Validate(
            draftContent,
            request.ContentIdea.Origin,
            request.RecentPosts,
            request.ReferenceDate);

        if (!validationResult.IsValid)
        {
            var reason = string.Join(" ", validationResult.FailedRules);
            _logger.LogWarning("Draft failed Capa 1 validation: {FailedRules}", reason);

            return (null, reason);
        }

        if (validationResult.Warnings.Count > 0)
        {
            _logger.LogWarning("Draft passed validation with warnings: {Warnings}", string.Join(" ", validationResult.Warnings));
        }

        if (!_options.EnableReviewer)
        {
            return (ToResult(draft, generatorPromptVersionId), null);
        }

        var reviewResult = await ReviewAsync(payload, draft, cancellationToken);

        if (reviewResult.IsApproved)
        {
            return (ToResult(draft, generatorPromptVersionId), null);
        }

        var rejectionReason = reviewResult.Notas ?? "El revisor rechazó el borrador sin especificar el motivo.";
        _logger.LogWarning("Draft rejected by LLM reviewer: {Notes}", rejectionReason);

        return (null, rejectionReason);
    }

    private async Task<ReviewerResponsePayload> ReviewAsync(
        GeneratorPayload sourcePayload,
        GeneratorResponsePayload draft,
        CancellationToken cancellationToken)
    {
        var reviewerSystemPrompt = ReviewerPromptBuilder.BuildSystemPrompt();
        await ResolveOrCreatePromptVersionAsync(PromptRole.Reviewer, reviewerSystemPrompt, cancellationToken);

        var reviewerPayload = ReviewerPromptBuilder.BuildPayload(sourcePayload, draft);

        return await _llmProvider.GenerateStructuredAsync<ReviewerResponsePayload>(
            reviewerSystemPrompt,
            reviewerPayload,
            cancellationToken: cancellationToken);
    }

    private async Task<PromptVersion> ResolveOrCreatePromptVersionAsync(
        PromptRole role,
        string content,
        CancellationToken cancellationToken)
    {
        var active = await _promptVersionRepository.GetActiveAsync(role, cancellationToken);

        if (active is not null && active.Content == content)
        {
            return active;
        }

        active?.Deactivate();

        var newVersion = new PromptVersion(Guid.NewGuid(), role, content, _dateTimeProvider.UtcNow);
        await _promptVersionRepository.AddAsync(newVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return newVersion;
    }

    private static string BuildCorrectionAddendum(string notes) =>
        $"ATENCIÓN: el intento anterior fue rechazado por el siguiente motivo. Corrígelo en esta respuesta: {notes}";

    private static GeneratedContentResult ToResult(GeneratorResponsePayload draft, Guid promptVersionId) =>
        new(draft.Hook, draft.Cuerpo, draft.Conclusion, draft.Cta, draft.Hashtags, draft.Fuentes, promptVersionId);
}
