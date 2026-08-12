using System.Net;

namespace DevContentEngine.Infrastructure.Tests.TestHelpers;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses;

    public List<string> RequestBodies { get; } = [];
    public List<Uri?> RequestUris { get; } = [];
    public List<string?> ContentTypes { get; } = [];

    public FakeHttpMessageHandler(params Func<HttpResponseMessage>[] responses)
    {
        _responses = new Queue<Func<HttpResponseMessage>>(responses);
    }

    public int CallCount { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CallCount++;

        RequestUris.Add(request.RequestUri);
        ContentTypes.Add(request.Content?.Headers.ContentType?.MediaType);

        RequestBodies.Add(request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken));

        return _responses.Count > 0
            ? _responses.Dequeue().Invoke()
            : new HttpResponseMessage(HttpStatusCode.InternalServerError);
    }
}
