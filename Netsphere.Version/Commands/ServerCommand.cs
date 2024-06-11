// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Unit;
using Netsphere.Relay;
using SimpleCommandLine;

namespace Netsphere.Version;

[SimpleCommand("server", Default = true)]
internal class ServerCommand : ISimpleCommandAsync<ServerOptions>
{
    public ServerCommand(ILogger<ServerCommand> logger, NetControl netControl, IRelayControl relayControl)
    {
        this.logger = logger;
        this.netControl = netControl;
        this.relayControl = relayControl;
    }

    public async Task RunAsync(ServerOptions options, string[] args)
    {
    }

    private readonly ILogger logger;
    private readonly NetControl netControl;
    private readonly IRelayControl relayControl;
}
