// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using ZenItz;
using ZenItz.Results;

namespace LP.Subcommands;

[SimpleCommand("add")]
public class ZenDirSubcommandAdd : ISimpleCommandAsync<ZenDirOptionsAdd>
{
    public ZenDirSubcommandAdd(ZenControl control)
    {
        this.Control = control;
    }

    public async Task Run(ZenDirOptionsAdd option, string[] args)
    {
        long cap = Zen.DefaultDirectoryCapacity;
        if (option.Capacity != 0)
        {
            cap = (long)option.Capacity * 1024 * 1024 * 1024;
        }

        await this.Control.Zen.Pause();
        var result = this.Control.Zen.IO.AddDirectory(option.Path, capacity: cap);
        this.Control.Zen.Restart();

        if (result == AddDictionaryResult.Success)
        {
            Logger.
        }
    }

    public ZenControl Control { get; set; }
}

public record ZenDirOptionsAdd
{
    [SimpleOption("path", Required = true, Description = "Directory path")]
    public string Path { get; init; } = string.Empty;

    [SimpleOption("capacity", description: "Directory capacity in GB")]
    public int Capacity { get; init; } = 0;
}
