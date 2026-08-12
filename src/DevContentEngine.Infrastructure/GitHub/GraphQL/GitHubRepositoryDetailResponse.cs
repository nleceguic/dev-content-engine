namespace DevContentEngine.Infrastructure.GitHub.GraphQL;

internal sealed record RepositoryDetailData(RepositoryDetailNode? Repository);

internal sealed record RepositoryDetailNode(
    string? Description,
    RepositoryLanguage? PrimaryLanguage,
    RepositoryTopicConnection RepositoryTopics,
    ReadmeBlob? Readme,
    ReadmeBlob? ReadmeLower,
    RepositoryTree? RootTree);

internal sealed record RepositoryLanguage(string Name);

internal sealed record RepositoryTopicConnection(IReadOnlyList<RepositoryTopicNode> Nodes);

internal sealed record RepositoryTopicNode(RepositoryTopicDetail Topic);

internal sealed record RepositoryTopicDetail(string Name);

internal sealed record ReadmeBlob(string? Text);

internal sealed record RepositoryTree(IReadOnlyList<TreeEntry> Entries);

internal sealed record TreeEntry(string Name, string Type);
