// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Core;

internal class NetIntervalTask
{
    private class IntervalTask : TaskCore
    {
        public IntervalTask(ThreadCoreBase? core, NetIntervalTask parent)
            : base(core, Process, false)
        {
            this.parent = parent;
        }

        private static async Task Process(object? parameter)
        {
            var core = (IntervalTask)parameter!;

            while (await core.Delay(1_000).ConfigureAwait(false))
            {
                await core.parent.netTerminal.IntervalTask(core.CancellationToken);
            }
        }

        private readonly NetIntervalTask parent;
    }

    public NetIntervalTask(NetTerminal netTerminal)
    {
        this.netTerminal = netTerminal;
    }

    private readonly NetTerminal netTerminal;
    private IntervalTask? task;

    public void Start(ThreadCoreBase parent)
    {
        this.task ??= new(parent, this);
        this.task.Start();
    }

    public void Stop()
    {
        this.task?.Terminate();
    }
}
