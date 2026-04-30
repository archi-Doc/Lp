// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Arc.Threading;

public class ExecutionCore : CancellationTokenSource, IDisposable
{
    #region FieldAndProperty

    private readonly ExecutionSignalHandler? executionSignalHandler;
    private TaskCompletionSource? completionSource;
    private ExecutionCore? parent;
    private ExecutionCore[] children = [];

    public ExecutionRoot Root { get; }

    /// <summary>
    /// Gets the owning <see cref="Arc.Threading.ExecutionStack"/> instance.
    /// </summary>
    public ExecutionStack? Stack { get; private set; }

    public ExecutionCore? Parent
    {
        get => this.parent;
        set
        {
            if (this.IsRoot ||
                this.parent == value)
            {
                return;
            }
            else if (value is null)
            {
                using (this.Root.SyncObject.EnterScope())
                {
                    this.parent?.RemoveChildInternal(this);
                }
            }
            else
            {
                value.AddChild(this);
            }
        }
    }

    public ExecutionCore[] Children => this.children;

    /// <summary>
    /// Gets the identifier of this execution within the owning <see cref="Stack"/>.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this execution is the root execution (<c>Id == 0</c>).
    /// </summary>
    public bool IsRoot => this.Id == 0;

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

    #endregion

    public static ExecutionCore? TryCreate(ExecutionCore parent, long id, ExecutionSignalHandler? executionSignalHandler = default)
    {
        var root = parent.Root;
        using (root.SyncObject.EnterScope())
        {
            if (root.IdToCore.ContainsKey(id))
            {// Already exists
                return null;
            }

            var core = new ExecutionCore(parent, id, executionSignalHandler);
            root.IdToCore.Add(id, core);
            return core;
        }
    }

    public ExecutionCore(ExecutionCore parent, ExecutionSignalHandler? executionSignalHandler = default)
    {
        this.Root = parent.Root;
        this.executionSignalHandler = executionSignalHandler;

        using (this.Root.SyncObject.EnterScope())
        {
            while (true)
            {
                var id = this.Root.Random.NextInt64();
                if (this.Root.IdToCore.TryAdd(id, this))
                {
                    this.Id = id;
                    break;
                }
            }

            parent.AddChildInternal(this);
        }
    }

    private protected ExecutionCore()
    {// Root
        this.Root = (ExecutionRoot)this;
        this.Id = 0;
        this.Root.IdToCore[0] = this;
    }

    private ExecutionCore(ExecutionCore parent, long id, ExecutionSignalHandler? executionSignalHandler)
    {// Create an ExecutionCore with the specified Id.
        this.Root = parent.Root;
        this.Id = id;
        this.executionSignalHandler = executionSignalHandler;

        parent.AddChildInternal(this);
    }

    public void TrySetCompleted()
        => this.GetCompletionSource().TrySetResult();

    public void SendSignal(ExecutionSignal signal)
        => this.executionSignalHandler?.Invoke(this, signal);

    public new void Cancel()
    {
        List<ExecutionCore>? list = default;
        while (true)
        {
            using (this.Root.SyncObject.EnterScope())
            {
                ProcessCancellationAndRemove(ref list, this, false);
            }

            if (list is null || list.Count == 0)
            {
                break;
            }

            foreach (var x in list)
            {
                ((CancellationTokenSource)x).Cancel();
            }
        }
    }

    public void TryCancel()
        => this.TryCancel(false);

    /// <summary>
    /// Removes this execution from its owning <see cref="Stack"/>.
    /// </summary>using (this.Root.SyncObject.EnterScope())
    public new void Dispose()
    {
        this.TryCancel(true);
        base.Dispose();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Execution {this.Id:x4}";
    }

    public void AddChild(ExecutionCore child)
    {
        if (this.Root != child.Root)
        {
            throw new InvalidOperationException("The parent and child objects must be created from the same Root.");
        }

        using (this.Root.SyncObject.EnterScope())
        {
            if (child.Parent == this)
            {
                return;
            }

            child.Parent?.RemoveChildInternal(child);
            this.AddChildInternal(child);
        }
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

    private static void ProcessCancellationAndRemove(ref List<ExecutionCore>? list, ExecutionCore core, bool remove)
    {
        var children = core.Children;
        foreach (var x in children)
        {
            ProcessCancellationAndRemove(ref list, x, remove);
        }

        if (!core.IsCancellationRequested)
        {
            list ??= new();
            list.Add(core);
        }

        if (remove)
        {
            core.Root.IdToCore.Remove(core.Id);
            core.parent?.RemoveChildInternal(core);
        }
    }

    [MemberNotNull(nameof(parent))]
    private void AddChildInternal(ExecutionCore child)
    {
        Debug.Assert(child.Parent is null);

        var newArray = new ExecutionCore[this.children.Length + 1];
        Array.Copy(this.children, newArray, this.children.Length);
        newArray[this.children.Length] = child;

        this.children = newArray;
        child.parent = this;
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
        child.parent = null;

        return true;
    }

    private void TryCancel(bool remove)
    {
        List<ExecutionCore>? list = default;
        while (true)
        {
            using (this.Root.SyncObject.EnterScope())
            {
                ProcessCancellationAndRemove(ref list, this, remove);
                remove = false;
            }

            if (list is null || list.Count == 0)
            {
                break;
            }

            foreach (var x in list)
            {
                try
                {
                    ((CancellationTokenSource)x).Cancel();
                }
                catch
                {
                }
            }
        }
    }
}
