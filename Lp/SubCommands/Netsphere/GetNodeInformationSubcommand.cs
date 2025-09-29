// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Net;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("get-node-information")]
public class GetNodeInformationSubcommand : ISimpleCommandAsync
{
    public GetNodeInformationSubcommand(ILogger<GetNodeInformationSubcommand> logger, IUserInterfaceService userInterfaceService, NetTerminal netTerminal)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netTerminal = netTerminal;
    }

    public async Task RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            return;
        }

        if (!NetNode.TryParseNetNode(this.logger, args[0], out var node))
        {
            return;
        }

        using (var connection = await this.netTerminal.Connect(node))
        {
            if (connection == null)
            {
                this.logger.TryGet()?.Log(Hashed.Error.Connect, node.ToString());
                return;
            }

            var service = connection.GetService<IBasalService>();
            var result = await service.GetNodeInformation();
            this.userInterfaceService.WriteLine(result);
        }
    }

    private readonly NetTerminal netTerminal;
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
}
