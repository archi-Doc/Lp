// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using LP.Subcommands;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

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
        this.NetControl.Terminal.CleanNetsphere();
        this.NetControl.Alternative?.CleanNetsphere();
    }

    public NetControl NetControl { get; set; }

    private ILogger<NetCleanSubcommand> logger;
}
