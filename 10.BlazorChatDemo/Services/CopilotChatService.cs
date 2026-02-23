using System.Runtime.CompilerServices;
using System.Text;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace BlazorChatDemo.Services;

public sealed class CopilotChatService : IAsyncDisposable
{
    private readonly CopilotClient _client;
    private readonly CopilotOptions _options;
    private readonly ILogger<CopilotChatService> _logger;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private bool _started;

    public string CurrentModel
    {
        get => _options.Model;
        set => _options.Model = value;
    }

    public IReadOnlyList<AIFunction>? Tools { get; set; }
    public string? SystemMessage { get; set; }

    public CopilotChatService(IOptions<CopilotOptions> optionsAccessor, ILogger<CopilotChatService> logger)
    {
        _options = optionsAccessor?.Value ?? new CopilotOptions();
        _logger = logger;
        _client = new CopilotClient(new CopilotClientOptions
        {
            CliPath = string.IsNullOrWhiteSpace(_options.CliPath) ? null : _options.CliPath,
            GithubToken = string.IsNullOrWhiteSpace(_options.GithubToken) ? null : _options.GithubToken,
            UseLoggedInUser = string.IsNullOrWhiteSpace(_options.GithubToken) && _options.UseLoggedInUser,
            Logger = logger
        });
    }

    private async Task EnsureStartedAsync()
    {
        if (_started) return;

        await _startLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_started) return;
            await _client.StartAsync().ConfigureAwait(false);
            _started = true;
        }
        finally
        {
            _startLock.Release();
        }
    }

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        await EnsureStartedAsync().ConfigureAwait(false);

        var config = new SessionConfig
        {
            Model = _options.Model,
            Streaming = true,
        };
        if (Tools is { Count: > 0 })
            config.Tools = Tools.ToList();
        if (!string.IsNullOrWhiteSpace(SystemMessage))
            config.SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = SystemMessage
            };

        await using var session = await _client.CreateSessionAsync(config).ConfigureAwait(false);

        var buffer = new StringBuilder();
        string? lastError = null;
        var idleTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var subscription = session.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    buffer.Append(delta.Data.DeltaContent);
                    break;
                case AssistantMessageEvent message:
                    _logger.LogDebug("[Message] Content length: {Len}", message.Data?.Content?.Length ?? 0);
                    break;
                case SessionErrorEvent error:
                    lastError = error.Data?.Message ?? "Unknown session error";
                    _logger.LogError("[SessionError] {Message}", lastError);
                    idleTcs.TrySetResult(false);
                    break;
                case SessionIdleEvent:
                    idleTcs.TrySetResult(true);
                    break;
            }
        });

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(2));

            try
            {
                await session.SendAsync(new MessageOptions { Prompt = prompt })
                    .WaitAsync(cts.Token).ConfigureAwait(false);
                await idleTcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("[AskAsync] Timed out. Buffer length: {Len}", buffer.Length);
            }

            if (buffer.Length > 0)
                return buffer.ToString();
            if (lastError != null)
                return $"Error: {lastError}";
            return "No response received from the AI model. Please try again.";
        }
        finally
        {
            subscription.Dispose();
        }
    }

    public async IAsyncEnumerable<string> AskStreamingAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await AskAsync(prompt, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(response))
            yield return response;
    }

    public async ValueTask DisposeAsync()
    {
        if (_started)
        {
            try { await _client.StopAsync().ConfigureAwait(false); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to stop Copilot client cleanly."); }
        }
        await _client.DisposeAsync().ConfigureAwait(false);
        _startLock.Dispose();
    }
}
