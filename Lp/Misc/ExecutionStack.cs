// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Arc.Threading;

public enum ExecutionSignal
{
    Cancel,
    Exit,
}

/// <summary>
/// Provides a thread-safe, stack-like collection of execution <see cref="Execution"/> objects.
/// </summary>
public class ExecutionStack
{
    // public const int DefaultMaxCount = 32;

    public delegate void ProcessSignalDelegate(Execution execution, ExecutionSignal executionSignal);

    /// <summary>
    /// Represents a removable execution entry within an <see cref="Stack"/>.
    /// </summary>
    /// <remarks>
    /// Disposing a <see cref="Execution"/> removes it from its owning <see cref="Stack"/>; it does not automatically cancel the execution.
    /// </remarks>
    public class Execution : IDisposable
    {
        #region FieldAndProperty

        private ProcessSignalDelegate? processSignal;
        private CancellationTokenSource cancellationTokenSource;
        private TaskCompletionSource? completionSource;
        // private Execution[]? children;

        /// <summary>
        /// Gets the owning <see cref="Arc.Threading.ExecutionStack"/> instance.
        /// </summary>
        public ExecutionStack Stack { get; private set; }

        public Execution? Parent { get; private set; }

        public Execution? Child { get; private set; }

        // public Execution[] Children => this.children ?? [];

        /// <summary>
        /// Gets the identifier of this execution within the owning <see cref="Stack"/>.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// Gets the <see cref="System.Threading.CancellationToken"/> associated with this execution.
        /// </summary>
        public CancellationToken CancellationToken => this.cancellationTokenSource.Token;

        public bool IsTerminated => this.CancellationToken.IsCancellationRequested;

        /// <summary>
        /// Gets a value indicating whether this execution can continue running.
        /// </summary>
        /// <remarks>
        /// Returns <see langword="false"/> after <see cref="CancellationToken"/> has been canceled.
        /// </remarks>
        public bool CanContinue => !this.CancellationToken.IsCancellationRequested;

        /// <summary>
        /// Gets a task that completes when this execution is explicitly marked as completed.
        /// </summary>
        public Task Completion => this.GetCompletionSource().Task;

        /// <summary>
        /// Gets a value indicating whether this execution is the root execution (<c>Id == 0</c>).
        /// </summary>
        public bool IsRoot => this.Id == 0;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Execution"/> class.
        /// </summary>
        /// <param name="executionStack">The owning <see cref="Arc.Threading.ExecutionStack"/>.</param>
        /// <param name="id">The execution identifier to assign.</param>
        /// <param name="processSignalHandler">An optional handler invoked when this execution processes an <see cref="ExecutionSignal"/>.</param>
        internal Execution(ExecutionStack executionStack, long id, ProcessSignalDelegate? processSignalHandler = default)
        {
            this.Stack = executionStack;
            this.Id = id;
            this.processSignal = processSignalHandler;
            this.cancellationTokenSource = CancellationTokenPool.Rent();
            this.completionSource = null;
        }

        public void ProcessSignal(ExecutionSignal signal)
            => this.processSignal?.Invoke(this, signal);

        public void TrySetResult()
            => this.GetCompletionSource().TrySetResult();

        public void TryCancel()
        {
            try
            {
                this.cancellationTokenSource.Cancel();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Removes this execution from its owning <see cref="Stack"/>.
        /// </summary>
        public void Dispose()
        {
            this.Stack.RemoveInternal(this);
            CancellationTokenPool.TryResetAndReturn(this.cancellationTokenSource);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Execution {this.Id}";
        }

        /// <summary>
        /// Add a child Execution.<br/>
        /// This must be called within ExecutionStack's synchronization lock (syncObject).
        /// </summary>
        /// <param name="child">A child execution.</param>
        internal void AddChild(Execution child)
        {
            if (this.Child is not null)
            {
                throw new InvalidOperationException();
            }

            this.Child = child;
            child.Parent = this;
        }

        private TaskCompletionSource GetCompletionSource()
        {
            var current = Volatile.Read(ref this.completionSource);
            if (current is not null)
            {
                return current;
            }

            var created = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return Interlocked.CompareExchange(ref this.completionSource, created, null) ?? created;
        }
    }

    /*/// <summary>
    /// Gets the root scope (created at construction time).
    /// </summary>
    /// <remarks>
    /// The root scope uses <c>Id = 0</c> and is not canceled by <see cref="CancelTop"/>.
    /// </remarks>
    public Scope Root { get; }

    public CancellationToken TopCancellationToken => this.Peek() is { } scope ? scope.CancellationToken : default;*/

    // public int MaxCount { get; }

    public int Count => this.list.Count;

    public bool IsEmpty => this.list.Count == 0;

    private readonly Lock syncObject = new();
    private readonly List<Execution> list = new();
    private readonly Xoshiro256StarStar random;

    public ExecutionStack()
    {
        // this.MaxCount = maxCount;
        this.random = new();
        // this.Root = new(this, 0);
        // this.list.Add(this.Root);
    }

    /// <summary>
    /// Creates and pushes a new <see cref="Execution"/> onto the stack.
    /// </summary>
    /// <param name="parent">Specify the parent execution.<br/>
    /// When the parent is deleted, this execution is automatically canceled and deleted as well.</param>
    /// <param name="processSignalHandler">An optional handler invoked when this execution processes an <see cref="ExecutionSignal"/>.</param>
    /// <returns>The newly created execution.</returns>
    public Execution Push(Execution? parent, ProcessSignalDelegate? processSignalHandler)
    {
        Execution execution;
        using (this.syncObject.EnterScope())
        {
            /*if (this.Count >= this.MaxCount)
            {
                throw new InvalidOperationException();
            }*/

            while (true)
            {
                var id = this.random.NextInt64();
                if (this.list.Find(x => x.Id == id) is null)
                {
                    execution = new Execution(this, id, processSignalHandler);
                    parent?.AddChild(execution);
                    this.list.Add(execution);
                    break;
                }
            }
        }

        return execution;
    }

    public Execution? TryPush(long id, Execution? parent, ProcessSignalDelegate? processSignalHandler)
    {
        using (this.syncObject.EnterScope())
        {
            if (this.list.Find(x => x.Id == id) is not null)
            {// this.Count >= this.MaxCount
                return null;
            }

            var execution = new Execution(this, id, processSignalHandler);
            parent?.AddChild(execution);
            this.list.Add(execution);
            return execution;
        }
    }

    /// <summary>
    /// Finds the first execution with the specified identifier.
    /// </summary>
    /// <param name="id">The execution identifier.</param>
    /// <returns>The matching execution; otherwise, <see langword="null"/>.</returns>
    public Execution? Find(long id)
    {
        using (this.syncObject.EnterScope())
        {
            return this.list.Find(x => x.Id == id);
        }
    }

    /// <summary>
    /// Gets the current top execution without removing it.
    /// </summary>
    /// <returns>The top execution; or <see langword="null"/> if the stack is empty.</returns>
    public Execution? Peek()
    {
        using (this.syncObject.EnterScope())
        {
            if (this.list.Count == 0)
            {
                return null;
            }
            else
            {
                return this.list[^1];
            }
        }
    }

    public bool Signal(ExecutionSignal signal)
    {
        var execution = this.Peek();
        if (execution is not null)
        {
            execution.ProcessSignal(signal);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool TrySetCompleted(long id, bool cancel = false)
    {
        var execution = this.Find(id);
        if (execution is not null)
        {
            execution.TrySetResult();
            if (cancel)
            {
                execution.TryCancel();
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public bool TryGetCancellationToken(long id, out CancellationToken cancellationToken)
    {
        var execution = this.Find(id);
        if (execution is not null)
        {
            cancellationToken = execution.CancellationToken;
            return true;
        }
        else
        {
            cancellationToken = default;
            return false;
        }
    }

    private void RemoveInternal(Execution item)
    {
        TemporaryList<Execution> toCancel = default;
        using (this.syncObject.EnterScope())
        {
            this.list.Remove(item);

            var e = item.Child;
            while (e is not null)
            {
                toCancel.Add(e);
                e = e.Child;
            }
        }

        if (toCancel.Count > 0)
        {
            foreach (var x in toCancel)
            {
                x.TryCancel();
            }
        }
    }

    /*/// <summary>
    /// Cancels the current top scope.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if a non-root top scope existed and its <see ref="System.Threading.CancellationTokenSource"/> was signaled;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool CancelTop()
    {
        var scope = this.Peek();
        if (scope is not null &&
            !scope.IsRoot)
        {
            scope.TryCancel();
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Removes the first execution with the specified identifier from the stack.
    /// </summary>
    /// <param name="id">The execution identifier.</param>
    /// <returns><see langword="true"/> if a execution was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(long id)
    {
        using (this.syncObject.EnterScope())
        {
            var index = this.list.FindIndex(x => x.Id == id);
            if (index >= 0)
            {// Foudddnd
                this.list.RemoveAt(index);
                return true;
            }
            else
            {// Not found
                return false;
            }
        }
    }*/
}
