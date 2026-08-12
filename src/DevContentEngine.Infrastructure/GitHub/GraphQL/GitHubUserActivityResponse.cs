namespace DevContentEngine.Infrastructure.GitHub.GraphQL;

internal sealed record GraphQlResponse<TData>(TData? Data, IReadOnlyList<GraphQlError>? Errors);

internal sealed record GraphQlError(string Message);

internal sealed record UserActivityData(UserActivityUser? User);

internal sealed record UserActivityUser(RepositoryConnection Repositories);

internal sealed record RepositoryConnection(IReadOnlyList<RepositoryNode> Nodes);

internal sealed record RepositoryNode(
    string Name,
    RepositoryOwner Owner,
    DateTimeOffset CreatedAt,
    DefaultBranchRef? DefaultBranchRef,
    PullRequestConnection PullRequests,
    IssueConnection Issues);

internal sealed record RepositoryOwner(string Login);

internal sealed record DefaultBranchRef(CommitTarget? Target);

internal sealed record CommitTarget(CommitHistoryConnection? History);

internal sealed record CommitHistoryConnection(IReadOnlyList<CommitNode> Nodes);

internal sealed record CommitNode(
    string Oid,
    string Message,
    DateTimeOffset CommittedDate,
    int Additions,
    int Deletions,
    int? ChangedFilesIfAvailable);

internal sealed record PullRequestConnection(IReadOnlyList<PullRequestNode> Nodes);

internal sealed record PullRequestNode(
    int Number,
    string Title,
    DateTimeOffset? MergedAt,
    FileConnection Files);

internal sealed record FileConnection(IReadOnlyList<FileNode> Nodes);

internal sealed record FileNode(string Path);

internal sealed record IssueConnection(IReadOnlyList<IssueNode> Nodes);

internal sealed record IssueNode(
    int Number,
    string Title,
    DateTimeOffset? ClosedAt,
    CommentConnection Comments);

internal sealed record CommentConnection(IReadOnlyList<CommentNode> Nodes);

internal sealed record CommentNode(string Body);
