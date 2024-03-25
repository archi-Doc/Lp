// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Core;

internal class NetCleaner
{
    private class CleanerTask : TaskCore
    {
        public CleanerTask(ThreadCoreBase? core, NetCleaner parent)
            : base(core, Process, false)
        {
            this.parent = parent;
        }

        private static async Task Process(object? parameter)
        {
            var core = (CleanerTask)parameter!;

            while (await core.Delay(1_000).ConfigureAwait(false))
            {
                core.parent.netTerminal.Clean();
            }
        }

        private readonly NetCleaner parent;
    }

    public NetCleaner(NetTerminal netTerminal)
    {
        this.netTerminal = netTerminal;
    }

    private readonly NetTerminal netTerminal;
    private CleanerTask? task;

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
