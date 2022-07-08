// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using LP.Unit;
using SimpleCommandLine;
using ZenItz;
using ZenItz.Results;

namespace ZenItz.Subcommands;

[SimpleCommand("add", Description = "Add zen directory.")]
public class ZenDirSubcommandAdd : ISimpleCommandAsync<ZenDirOptionsAdd>
{
    public ZenDirSubcommandAdd(ZenControl control)
    {
        this.ZenControl = control;
    }

    public async Task Run(ZenDirOptionsAdd option, string[] args)
    {
        long cap = Zen.DefaultDirectoryCapacity;
        if (option.Capacity != 0)
        {
            cap = (long)option.Capacity * 1024 * 1024 * 1024;
        }

        await this.ZenControl.Zen.Pause();
        var result = this.ZenControl.Zen.IO.AddDirectory(option.Path, capacity: cap);
        this.ZenControl.Zen.Restart();

        if (result == AddDictionaryResult.Success)
        {
            Logger.Subcommand.Information($"Directory added: {option.Path}");
            Console.WriteLine();
            // await this.ZenControl.SubcommandParser.ParseAndRunAsync("zendir ls");
        }
    }

    public ZenControl ZenControl { get; set; }
}

public record ZenDirOptionsAdd
{
    [SimpleOption("path", Required = true, Description = "Directory path")]
    public string Path { get; init; } = string.Empty;

    [SimpleOption("capacity", description: "Directory capacity in GB")]
    public int Capacity { get; init; } = 0;
}
