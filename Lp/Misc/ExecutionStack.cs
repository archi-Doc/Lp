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
/// Provides a thread-safe, stack-like collection of execution <see cref="Scope"/> objects.
/// </summary>
public class ExecutionStack
{
    public delegate void ProcessSignalHandler(Scope scope, ExecutionSignal executionSignal);

    /// <summary>
    /// Represents a removable scope entry within an <see cref="ExecutionStack"/>.
    /// </summary>
    /// <remarks>
    /// Disposing a <see cref="Scope"/> removes it from its owning <see cref="ExecutionStack"/>; it does not
    /// automatically cancel the scope.
    /// </remarks>
    public class Scope : IDisposable
    {
        /// <summary>
        /// Gets the owning <see cref="Arc.Threading.ExecutionStack"/> instance.
        /// </summary>
        public ExecutionStack ExecutionStack { get; }

        /// <summary>
        /// Gets the identifier of this scope within the owning <see cref="ExecutionStack"/>.
        /// </summary>
        public long Id { get; }

        private readonly ProcessSignalHandler? processSignalHandler;

        /// <summary>
        /// Gets the <see cref="System.Threading.CancellationTokenSource"/> associated with this scope.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; }

        /// <summary>
        /// Gets the <see cref="System.Threading.CancellationToken"/> associated with this scope.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        private TaskCompletionSource? completionSource;

        public Task Completion => this.GetCompletionSource().Task;

        internal TaskCompletionSource GetCompletionSource()
        {
            var current = Volatile.Read(ref this.completionSource);
            if (current is not null)
            {
                return current;
            }

            var created = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return Interlocked.CompareExchange(ref this.completionSource, created, null) ?? created;
        }

        /// <summary>
        /// Gets a value indicating whether this scope is the root scope (<c>Id == 0</c>).
        /// </summary>
        public bool IsRoot => this.Id == 0;

        public bool CanContinue => !this.CancellationToken.IsCancellationRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scope"/> class.
        /// </summary>
        /// <param name="executionStack">The owning <see cref="Arc.Threading.ExecutionStack"/>.</param>
        /// <param name="id">The scope identifier to assign.</param>
        /// <param name="processSignalHandler">An optional handler invoked when this scope processes an <see cref="ExecutionSignal"/>.</param>
        internal Scope(ExecutionStack executionStack, long id, ProcessSignalHandler? processSignalHandler = default)
        {
            this.ExecutionStack = executionStack;
            this.Id = id;
            this.processSignalHandler = processSignalHandler;
            this.CancellationTokenSource = CancellationTokenPool.Rent();
            this.CancellationToken = this.CancellationTokenSource.Token;
            // this.TaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void ProcessSignal(ExecutionSignal signal)
            => this.processSignalHandler?.Invoke(this, signal);

        /// <summary>
        /// Removes this scope from its owning <see cref="ExecutionStack"/>.
        /// </summary>
        public void Dispose()
        {
            this.ExecutionStack.Remove(this);
            CancellationTokenPool.TryResetAndReturn(this.CancellationTokenSource);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var isCanceled = this.CancellationTokenSource.IsCancellationRequested ? " Canceled" : string.Empty;
            return $"Execution Scope {this.Id}{isCanceled}";
        }
    }

    /*/// <summary>
    /// Gets the root scope (created at construction time).
    /// </summary>
    /// <remarks>
    /// The root scope uses <c>Id = 0</c> and is not canceled by <see cref="CancelTop"/>.
    /// </remarks>
    public Scope Root { get; } */

    public CancellationToken TopCancellationToken => this.Peek() is { } scope ? scope.CancellationToken : default;

    private readonly Lock syncObject = new();
    private readonly List<Scope> list = new();
    private readonly Xoshiro256StarStar random;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionStack"/> class and creates <see cref="Root"/>.
    /// </summary>
    public ExecutionStack()
    {
        this.random = new();
        // this.Root = new(this, 0);
        // this.list.Add(this.Root);
    }

    /// <summary>
    /// Creates and pushes a new <see cref="Scope"/> onto the stack.
    /// </summary>
    /// <param name="processSignalHandler">An optional handler invoked when this scope processes an <see cref="ExecutionSignal"/>.</param>
    /// <returns>The newly created scope.</returns>
    public Scope Push(ProcessSignalHandler? processSignalHandler)
    {
        Scope newScope;
        using (this.syncObject.EnterScope())
        {
            while (true)
            {
                var id = (long)this.random.NextUInt64();
                if (this.list.Find(x => x.Id == id) is not null)
                {
                    continue;
                }

                newScope = new Scope(this, id, processSignalHandler);
                this.list.Add(newScope);
            }
        }

        return newScope;
    }

    public Scope? TryPush(long id, ProcessSignalHandler? processSignalHandler)
    {
        using (this.syncObject.EnterScope())
        {
            if (this.list.Find(x => x.Id == id) is not null)
            {
                return null;
            }

            var newScope = new Scope(this, id, processSignalHandler);
            this.list.Add(newScope);
            return newScope;
        }
    }

    /// <summary>
    /// Finds the first scope with the specified identifier.
    /// </summary>
    /// <param name="id">The scope identifier.</param>
    /// <returns>The matching scope; otherwise, <see langword="null"/>.</returns>
    public Scope? Find(long id)
    {
        using (this.syncObject.EnterScope())
        {
            return this.list.Find(x => x.Id == id);
        }
    }

    /// <summary>
    /// Gets the current top scope without removing it.
    /// </summary>
    /// <returns>The top scope; or <see langword="null"/> if the stack is empty.</returns>
    public Scope? Peek()
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

    /// <summary>
    /// Cancels the current top scope, if it exists and is not the <see cref="Root"/> scope.
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
            scope.CancellationTokenSource.Cancel();
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool Signal(ExecutionSignal signal)
    {
        var scope = this.Peek();
        if (scope is not null)
        {
            scope.ProcessSignal(signal);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Removes the specified scope from the stack.
    /// </summary>
    /// <param name="item">The scope instance to remove.</param>
    /// <returns><see langword="true"/> if the scope was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(Scope item)
    {
        using (this.syncObject.EnterScope())
        {
            return this.list.Remove(item);
        }
    }

    /// <summary>
    /// Removes the first scope with the specified identifier from the stack.
    /// </summary>
    /// <param name="id">The scope identifier.</param>
    /// <returns><see langword="true"/> if a scope was removed; otherwise, <see langword="false"/>.</returns>
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
    }

    public bool TrySetCompleted(long id)
    {
        var scope = this.Peek();
        if (scope is not null)
        {
            scope.GetCompletionSource().TrySetResult();
            return true;
        }
        else
        {
            return false;
        }
    }
}
