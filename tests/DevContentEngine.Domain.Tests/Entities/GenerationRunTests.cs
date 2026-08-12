using DevContentEngine.Domain.Entities;
using DevContentEngine.Domain.Events;
using FluentAssertions;

namespace DevContentEngine.Domain.Tests.Entities;

public class GenerationRunTests
{
    private static readonly DateTime StartedAt = new(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Fail_raises_a_GenerationRunFailedEvent_with_the_run_id_and_trimmed_message()
    {
        var run = new GenerationRun(Guid.NewGuid(), StartedAt);

        run.Fail("  GitHub API is unavailable  ", StartedAt.AddMinutes(1));

        run.DomainEvents.Should().ContainSingle();

        var domainEvent = run.DomainEvents.Single().Should().BeOfType<GenerationRunFailedEvent>().Subject;
        domainEvent.GenerationRunId.Should().Be(run.Id);
        domainEvent.ErrorMessage.Should().Be("GitHub API is unavailable");
    }

    [Fact]
    public void CompleteSuccessfully_does_not_raise_a_GenerationRunFailedEvent()
    {
        var run = new GenerationRun(Guid.NewGuid(), StartedAt);

        run.CompleteSuccessfully(Enums.ContentPath.GitHubPath, Guid.NewGuid(), StartedAt.AddMinutes(1));

        run.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CompleteWithoutContent_stores_the_trimmed_reason_as_the_ErrorMessage()
    {
        var run = new GenerationRun(Guid.NewGuid(), StartedAt);

        run.CompleteWithoutContent(StartedAt.AddMinutes(1), "  No trend candidates were found.  ");

        run.Status.Should().Be(Enums.GenerationRunStatus.NoContentGenerated);
        run.ErrorMessage.Should().Be("No trend candidates were found.");
    }

    [Fact]
    public void CompleteWithoutContent_leaves_ErrorMessage_null_when_no_reason_is_given()
    {
        var run = new GenerationRun(Guid.NewGuid(), StartedAt);

        run.CompleteWithoutContent(StartedAt.AddMinutes(1));

        run.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void CompleteWithoutContent_raises_a_GenerationRunCompletedWithoutContentEvent_with_the_run_id_and_trimmed_reason()
    {
        var run = new GenerationRun(Guid.NewGuid(), StartedAt);

        run.CompleteWithoutContent(StartedAt.AddMinutes(1), "  No trend candidates were found.  ");

        run.DomainEvents.Should().ContainSingle();

        var domainEvent = run.DomainEvents.Single().Should().BeOfType<GenerationRunCompletedWithoutContentEvent>().Subject;
        domainEvent.GenerationRunId.Should().Be(run.Id);
        domainEvent.Reason.Should().Be("No trend candidates were found.");
    }
}
