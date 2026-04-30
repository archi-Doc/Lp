// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Arc.Threading;

public class ExecutionCore : CancellationTokenSource, IDisposable
{
    #region FieldAndProperty

    private readonly ExecutionSignalHandler? executionSignalHandler;
    private TaskCompletionSource? completionSource;
    private ExecutionCore parent;
    private ExecutionCore[] children = [];

    public ExecutionRoot Root { get; }

    /// <summary>
    /// Gets the owning <see cref="Arc.Threading.ExecutionStack"/> instance.
    /// </summary>
    public ExecutionStack? Stack { get; private set; }

    public ExecutionCore Parent
    {
        get => this.parent;
        set => value.AddChild(this);
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

    public ExecutionCore(ExecutionCore parent, ExecutionSignalHandler? executionSignalHandler = default)
    {
        this.Root = parent.Root;
        this.executionSignalHandler = executionSignalHandler;

        using (this.Root.SyncObject.EnterScope())
        {
            while (true)
            {
                var id = this.Root.Random.NextInt64();
                if (!this.Root.IdToCore.ContainsKey(id))
                {
                    this.Id = id;
                    break;
                }
            }

            this.AddChildInternal(this);
        }
    }

    public void SendSignal(ExecutionSignal signal)
        => this.executionSignalHandler?.Invoke(this, signal);

    public new void Cancel()
    {
        List<ExecutionCore>? list = default;

        while (true)
        {
            this.CreateCancelList(ref list);
            if (list is null ||
                list.Count == 0)
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
    {
        List<ExecutionCore>? list = default;

        while (true)
        {
            using (this.Root.SyncObject.EnterScope())
            {
                this.CreateCancelList(ref list);
            }

            if (list is null ||
                list.Count == 0)
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

    private void CreateCancelList(ref List<ExecutionCore>? list)
    {
        var children = this.Children;
        foreach (var x in children)
        {
            this.CreateCancelList(ref list);
        }

        if (!this.IsCancellationRequested)
        {
            list ??= new();
            list.Add(this);
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
        return $"Execution {this.Id:x4}";
    }

    public void AddChild(ExecutionCore child)
    {
        Debug.Assert(this.Root == child.Root);

        using (this.Root.SyncObject.EnterScope())
        {
            if (child.Parent == this)
            {
                return;
            }

            child.Parent.RemoveChildInternal(child);
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
        child.parent = default!;

        return true;
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
