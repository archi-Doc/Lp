// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.Runtime.Internal;

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
    public const int DefaultMaxCount = 32;

    public delegate void ProcessSignalDelegate(Execution execution, ExecutionSignal executionSignal);

    /// <summary>
    /// Represents a removable execution entry within an <see cref="ExecutionStack"/>.
    /// </summary>
    /// <remarks>
    /// Disposing a <see cref="Execution"/> removes it from its owning <see cref="ExecutionStack"/>; it does not automatically cancel the execution.
    /// </remarks>
    public class Execution : IDisposable
    {
        private ProcessSignalDelegate? processSignal;
        private CancellationTokenSource cancellationTokenSource;
        private TaskCompletionSource? completionSource;
        private Execution? parent;
        private List<Execution>? children;

        /// <summary>
        /// Gets the owning <see cref="Arc.Threading.ExecutionStack"/> instance.
        /// </summary>
        public ExecutionStack ExecutionStack { get; private set; }

        /// <summary>
        /// Gets the identifier of this execution within the owning <see cref="ExecutionStack"/>.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// Gets the <see cref="System.Threading.CancellationToken"/> associated with this execution.
        /// </summary>
        public CancellationToken CancellationToken { get; private set; }

        public Task Completion => this.GetCompletionSource().Task;

        /// <summary>
        /// Gets a value indicating whether this execution is the root execution (<c>Id == 0</c>).
        /// </summary>
        public bool IsRoot => this.Id == 0;

        public bool CanContinue => !this.CancellationToken.IsCancellationRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="Execution"/> class.
        /// </summary>
        /// <param name="executionStack">The owning <see cref="Arc.Threading.ExecutionStack"/>.</param>
        /// <param name="id">The execution identifier to assign.</param>
        /// <param name="processSignalHandler">An optional handler invoked when this execution processes an <see cref="ExecutionSignal"/>.</param>
        internal Execution(ExecutionStack executionStack, long id, ProcessSignalDelegate? processSignalHandler = default)
        {
            this.ExecutionStack = executionStack;
            this.Id = id;
            this.processSignal = processSignalHandler;
            this.cancellationTokenSource = CancellationTokenPool.Rent();
            this.CancellationToken = this.cancellationTokenSource.Token;
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
        /// Removes this execution from its owning <see cref="ExecutionStack"/>.
        /// </summary>
        public void Dispose()
        {
            this.ExecutionStack.Remove(this);
            CancellationTokenPool.TryResetAndReturn(this.cancellationTokenSource);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Execution {this.Id}";
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

    public int MaxCount { get; }

    public int Count => this.list.Count;

    public bool IsEmpty => this.list.Count == 0;

    private readonly Lock syncObject = new();
    private readonly List<Execution> list = new();
    private readonly Xoshiro256StarStar random;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionStack"/> class.
    /// </summary>
    /// <param name="maxCount">
    /// The maximum number of <see cref="Execution"/> instances that can be stored in the stack at the same time.
    /// </param>
    public ExecutionStack(int maxCount = DefaultMaxCount)
    {
        this.MaxCount = maxCount;
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
        Execution newScope;
        using (this.syncObject.EnterScope())
        {
            if (this.Count >= this.MaxCount)
            {
                throw new InvalidOperationException();
            }

            while (true)
            {
                var id = this.random.NextInt64();
                if (this.list.Find(x => x.Id == id) is null)
                {
                    newScope = new Execution(this, id, processSignalHandler);
                    this.list.Add(newScope);
                    break;
                }
            }
        }

        return newScope;
    }

    public Execution? TryPush(long id, ProcessSignalDelegate? processSignalHandler)
    {
        using (this.syncObject.EnterScope())
        {
            if (this.Count >= this.MaxCount ||
                this.list.Find(x => x.Id == id) is not null)
            {
                return null;
            }

            var newScope = new Execution(this, id, processSignalHandler);
            this.list.Add(newScope);
            return newScope;
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

    private bool Remove(Execution item)
    {
        using (this.syncObject.EnterScope())
        {
            return this.list.Remove(item);
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
