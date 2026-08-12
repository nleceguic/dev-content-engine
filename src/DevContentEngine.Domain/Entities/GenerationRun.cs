using DevContentEngine.Domain.Common;
using DevContentEngine.Domain.Enums;
using DevContentEngine.Domain.Events;

namespace DevContentEngine.Domain.Entities;

public sealed class GenerationRun : Entity
{
    public DateTime StartedAt { get; }
    public DateTime? FinishedAt { get; private set; }
    public GenerationRunStatus? Status { get; private set; }
    public ContentPath? ChosenPath { get; private set; }
    public int? TokensUsed { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? ResultingPostId { get; private set; }

    public bool IsFinished => Status is not null;

    private GenerationRun()
    {
    }

    public GenerationRun(Guid id, DateTime startedAt)
        : base(id)
    {
        StartedAt = startedAt;
    }

    public void CompleteSuccessfully(ContentPath chosenPath, Guid resultingPostId, DateTime finishedAt, int? tokensUsed = null)
    {
        EnsureNotFinished();
        EnsureFinishedAtIsValid(finishedAt);

        if (!Enum.IsDefined(chosenPath))
            throw new ArgumentOutOfRangeException(nameof(chosenPath), chosenPath, "Unknown content path.");

        if (resultingPostId == Guid.Empty)
            throw new ArgumentException("A successful run must reference the resulting post.", nameof(resultingPostId));

        Status = GenerationRunStatus.Success;
        ChosenPath = chosenPath;
        ResultingPostId = resultingPostId;
        TokensUsed = tokensUsed;
        FinishedAt = finishedAt;
    }

    public void CompleteWithoutContent(DateTime finishedAt, string? reason = null, int? tokensUsed = null)
    {
        EnsureNotFinished();
        EnsureFinishedAtIsValid(finishedAt);

        Status = GenerationRunStatus.NoContentGenerated;
        ErrorMessage = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        TokensUsed = tokensUsed;
        FinishedAt = finishedAt;

        AddDomainEvent(new GenerationRunCompletedWithoutContentEvent(Id, ErrorMessage));
    }

    public void Fail(string errorMessage, DateTime finishedAt, int? tokensUsed = null)
    {
        EnsureNotFinished();
        EnsureFinishedAtIsValid(finishedAt);

        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("A failed run must include an error message.", nameof(errorMessage));

        Status = GenerationRunStatus.Failed;
        ErrorMessage = errorMessage.Trim();
        TokensUsed = tokensUsed;
        FinishedAt = finishedAt;

        AddDomainEvent(new GenerationRunFailedEvent(Id, ErrorMessage));
    }

    private void EnsureNotFinished()
    {
        if (IsFinished)
            throw new InvalidOperationException("This generation run has already finished.");
    }

    private void EnsureFinishedAtIsValid(DateTime finishedAt)
    {
        if (finishedAt < StartedAt)
            throw new ArgumentException("FinishedAt cannot be earlier than StartedAt.", nameof(finishedAt));
    }
}
