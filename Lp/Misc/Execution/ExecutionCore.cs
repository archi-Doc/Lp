// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Arc.Threading;

/// <summary>
/// Represents a removable execution entry within an <see cref="Stack"/>.
/// </summary>
/// <remarks>
/// Disposing a <see cref="ExecutionCore"/> removes it from its owning <see cref="Stack"/>; it does not automatically cancel the execution.
/// </remarks>
public class ExecutionCore : CancellationTokenSource, IDisposable
{
    #region FieldAndProperty

    private ExecutionSignalHandler? processSignal;
    private TaskCompletionSource? completionSource;
    private ExecutionCore[] children = [];

    public ExecutionRoot Root { get; }

    /// <summary>
    /// Gets the owning <see cref="Arc.Threading.ExecutionStack"/> instance.
    /// </summary>
    public ExecutionStack? Stack { get; private set; }

    public ExecutionCore? Parent { get; private set; }

    public ExecutionCore[] Children => this.children;

    /// <summary>
    /// Gets the identifier of this execution within the owning <see cref="Stack"/>.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Gets the <see cref="System.Threading.CancellationToken"/> associated with this execution.
    /// </summary>
    public CancellationToken CancellationToken => this.Token;

    public bool IsTerminated => this.IsCancellationRequested;

    /// <summary>
    /// Gets a value indicating whether this execution can continue running.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="false"/> after <see cref="CancellationToken"/> has been canceled.
    /// </remarks>
    public bool CanContinue => !this.IsCancellationRequested;

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
    /// Initializes a new instance of the <see cref="ExecutionCore"/> class.
    /// </summary>
    /// <param name="id">The execution identifier to assign.</param>
    /// <param name="executionSignalHandler">An optional handler invoked when this execution processes an <see cref="ExecutionSignal"/>.</param>
    internal ExecutionCore(ExecutionCore parent, ExecutionSignalHandler? executionSignalHandler = default)
    {
        this.Root = parent.Root;
        parent.AddChild(this);
        this.Id = id;
        this.processSignal = executionSignalHandler;
        this.completionSource = null;
    }

    public void Signal(ExecutionSignal signal)
        => this.processSignal?.Invoke(this, signal);

    public new void Cancel()
    {
        var context = this.FindLeaf();
        while (true)
        {
            ((CancellationTokenSource)context!).Cancel();
            if (context == this)
            {
                break;
            }
            else
            {
                context = context.Parent;
            }
        }
    }

    public void TryCancel()
    {
        var context = this.FindLeaf();
        while (context is not null)
        {
            try
            {
                ((CancellationTokenSource)context).Cancel();
            }
            catch
            {
            }

            if (context == this)
            {
                break;
            }
            else
            {
                context = context.Parent;
            }
        }
    }

    /// <summary>
    /// Removes this execution from its owning <see cref="Stack"/>.
    /// </summary>
    public new void Dispose()
    {
        if (!this.IsCancellationRequested)
        {
            this.TryCancel();
        }

        base.Dispose();
        this.Stack.RemoveInternal(this);
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
    public void AddChild(ExecutionCore child)
    {
        using (this.Root.syncObject.EnterScope())
        {
            if (child.Parent is { } parent)
            {
                parent.RemoveChildInternal(child);
            }

            this.AddChildInternal(child);
        }

        if (this.Child is not null)
        {
            throw new InvalidOperationException();
        }

        this.Child = child;
        child.Parent = this;
    }

    private void AddChildInternal(ExecutionCore child)
    {
        var length = this.children.Length;
        Array.Resize(ref this.children, length + 1);
        array[length] = item;
    }

    private bool RemoveChildInternal(ExecutionCore child)
    {
        var index = Array.IndexOf(this.children, child);
        if (index < 0)
        {
            return false;
        }

        var newArray = new ExecutionCore[this.children.Length - 1];
        if (index > 0)
        {
            Array.Copy(this.children, 0, newArray, 0, index);
        }

        if (index < this.children.Length - 1)
        {
            Array.Copy(this.children, index + 1, newArray, index, this.children.Length - index - 1);
        }

        this.children = newArray;
        child.Parent = null;

        return true;
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ExecutionCore FindLeaf()
    {
        var context = this;
        while (true)
        {
            if (context.Child is null)
            {
                return context;
            }
            else
            {
                context = context.Child;
            }
        }
    }
}
