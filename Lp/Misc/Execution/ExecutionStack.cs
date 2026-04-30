// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Arc.Threading;

public class ExecutionStack
{
    #region FieldAndProperty

    private readonly Lock syncObject = new();
    private readonly List<ExecutionCore> list = new();

    public int Count => this.list.Count;

    public bool IsEmpty => this.list.Count == 0;

    public ExecutionCore? TopCore
    {
        get
        {
            using (this.syncObject.EnterScope())
            {
                return this.list.Count == 0 ? null : this.list[0];
            }
        }
    }

    public ExecutionCore? BottomCore
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
    }

    /// <summary>
    /// Creates and pushes a new <see cref="ExecutionCore"/> onto the stack.
    /// </summary>
    /// <param name="parent">Specify the parent execution.<br/>
    /// When the parent is deleted, this execution is automatically canceled and deleted as well.</param>
    /// <param name="processSignalHandler">An optional handler invoked when this execution processes an <see cref="ExecutionSignal"/>.</param>
    /// <returns>The newly created execution.</returns>
    public ExecutionCore Push(ExecutionCore parent, ExecutionSignalHandler? processSignalHandler)
    {
        var core = new ExecutionCore(parent, processSignalHandler);
        using (this.syncObject.EnterScope())
        {
            this.list.Add(core);
        }

        return core;
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

    public bool SignalBottom(ExecutionSignal signal)
    {
        var execution = this.BottomCore;
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
}
