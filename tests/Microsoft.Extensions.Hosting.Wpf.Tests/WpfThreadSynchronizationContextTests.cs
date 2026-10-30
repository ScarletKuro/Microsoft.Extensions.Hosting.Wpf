using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.Threading;
using Xunit;

namespace Microsoft.Extensions.Hosting.Wpf.Tests;

// The forwarding context JoinableTaskContext switches with. The scenarios only reach Post (SwitchToMainThreadAsync).
public sealed class WpfThreadSynchronizationContextTests
{
    [Fact]
    public void ForwardsToTheUiThreadContextWhenUsedNotWhenCreated()
    {
        var thread = new FakeWpfThread();
        // Created before the UI thread runs, like a JoinableTaskContext resolved by a hosted service constructor.
        var context = Create(thread);
        var uiContext = new RecordingSynchronizationContext();
        thread.Context = uiContext;

        context.Post(state => uiContext.Calls.Add($"Ran {state}"), "posted");
        context.Send(state => uiContext.Calls.Add($"Ran {state}"), "sent");

        Assert.Equal(new[] { "Post", "Ran posted", "Send", "Ran sent" }, uiContext.Calls);
        // The base CreateCopy would return a plain context that runs callbacks on the thread pool.
        Assert.Same(context, context.CreateCopy());
    }

    [Fact]
    public void UseBeforeTheUiThreadStartsFailsForAnotherIWpfThread()
    {
        var context = Create(new FakeWpfThread());

        Assert.Throws<InvalidOperationException>(() => context.Post(_ => { }, null));
        Assert.Throws<InvalidOperationException>(() => context.Send(_ => { }, null));
    }

    [Fact]
    public void PostGoesThroughTheLibrarysThreadWhichQueuesUntilWpfStarts()
    {
        // WpfThread queues work posted before the application exists instead of throwing: JoinableTaskFactory terminates the
        // process (Environment.FailFast) when Post throws. The end-to-end behavior is covered by ScenarioTests (EarlySwitchRan).
        var thread = new PostingWpfThread();
        var context = Create(thread);

        context.Post(_ => { }, "posted");

        Assert.Equal(new object?[] { "posted" }, thread.Posted);
    }

    private static WpfThreadSynchronizationContext Create(IWpfThread thread) => new(thread);

    private sealed class PostingWpfThread : IWpfThread, Internal.IUiThreadPoster
    {
        public List<object?> Posted { get; } = new();

        public IWpfContext WpfContext => throw new NotSupportedException();

        public Thread MainThread => throw new NotSupportedException();

        // Not started: the context must not be used for Post.
        public SynchronizationContext SynchronizationContext => throw new InvalidOperationException("WPF Thread was not started.");

        public void HandleApplicationExit() => throw new NotSupportedException();

        public void Post(SendOrPostCallback callback, object? state) => Posted.Add(state);
    }

    private sealed class FakeWpfThread : IWpfThread
    {
        public SynchronizationContext? Context { get; set; }

        public IWpfContext WpfContext => throw new NotSupportedException();

        public Thread MainThread => throw new NotSupportedException();

        // Like WpfThread: there is no context until the UI thread runs.
        public SynchronizationContext SynchronizationContext => Context ?? throw new InvalidOperationException("WPF Thread was not started.");

        public void HandleApplicationExit() => throw new NotSupportedException();
    }

    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        public List<string> Calls { get; } = new();

        public override void Post(SendOrPostCallback d, object? state)
        {
            Calls.Add("Post");
            d(state);
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Calls.Add("Send");
            d(state);
        }
    }
}
