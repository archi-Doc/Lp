// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("netclean")]
public class NetCleanSubcommand : ISimpleCommandAsync
{
    public NetCleanSubcommand(ILogger<NetCleanSubcommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.NetControl = netControl;
    }

    public async Task RunAsync(string[] args)
    {
        // this.NetControl.TerminalObsolete.CleanNetsphere();
        // this.NetControl.AlternativeObsolete?.CleanNetsphere();
    }

    public NetControl NetControl { get; set; }

    private ILogger<NetCleanSubcommand> logger;
}
