// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Collections;

namespace Arc.Threading;

/// <summary>
/// Provides a thread-safe, stack-like collection of execution <see cref="ExecutionCore"/> objects.
/// </summary>
public class ExecutionStack
{
    #region FieldAndProperty

    private readonly Lock syncObject = new();
    private readonly List<ExecutionCore> list = new();
    private readonly Xoshiro256StarStar random;

    public int Count => this.list.Count;

    public bool IsEmpty => this.list.Count == 0;

    public ExecutionCore? TopContext
    {
        get
        {
            using (this.syncObject.EnterScope())
            {
                return this.list.Count == 0 ? null : this.list[0];
            }
        }
    }

    public ExecutionCore? BottomContext
    {
        get
        {
            using (this.syncObject.EnterScope())
            {
                return this.list.Count == 0 ? null : this.list[^1];
            }
        }
    }

    #endregion

    public ExecutionStack()
    {
        // this.MaxCount = maxCount;
        this.random = new();
        // this.Root = new(this, 0);
        // this.list.Add(this.Root);
    }

    /// <summary>
    /// Creates and pushes a new <see cref="ExecutionCore"/> onto the stack.
    /// </summary>
    /// <param name="parent">Specify the parent execution.<br/>
    /// When the parent is deleted, this execution is automatically canceled and deleted as well.</param>
    /// <param name="processSignalHandler">An optional handler invoked when this execution processes an <see cref="ExecutionSignal"/>.</param>
    /// <returns>The newly created execution.</returns>
    public ExecutionCore Push(ExecutionCore? parent, ExecutionSignalHandler? processSignalHandler)
    {
        ExecutionCore execution;
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
                    execution = new ExecutionCore(this, id, processSignalHandler);
                    parent?.AddChild(execution);
                    this.list.Add(execution);
                    break;
                }
            }
        }

        return execution;
    }

    public ExecutionCore? TryPush(long id, ExecutionCore? parent, ExecutionSignalHandler? processSignalHandler)
    {
        using (this.syncObject.EnterScope())
        {
            if (this.list.Find(x => x.Id == id) is not null)
            {// this.Count >= this.MaxCount
                return null;
            }

            var execution = new ExecutionCore(this, id, processSignalHandler);
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
    public ExecutionCore? Find(long id)
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
    public ExecutionCore? Peek()
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

    public bool SignalBottom(ExecutionSignal signal)
    {
        var execution = this.Peek();
        if (execution is not null)
        {
            execution.SendSignal(signal);
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
            execution.GetCompletionSource().TrySetResult();
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

    private void RemoveInternal(ExecutionCore item)
    {
        TemporaryList<ExecutionCore> toCancel = default;
        using (this.syncObject.EnterScope())
        {
            item.Parent?.Child = null;
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
