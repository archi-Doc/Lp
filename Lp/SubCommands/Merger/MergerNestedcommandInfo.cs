// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("info")]
public class MergerNestedcommandInfo : ISimpleCommandAsync
{
    public MergerNestedcommandInfo(ILogger<MergerNestedcommandInfo> logger, NetTerminal terminal, MergerNestedcommand nestedcommand)
    {
        this.logger = logger;
        this.terminal = terminal;
        this.nestedcommand = nestedcommand;
    }

    public async Task RunAsync(string[] args)
    {
        /*var authority = await this.authority.GetKeyAsync(options.Authority);
        if (authority == null)
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, options.Authority);
            return;
        }*/

        this.logger.TryGet()?.Log(string.Empty);
        using (var terminal = await this.terminal.Connect(this.nestedcommand.Node))
        {
            if (terminal == null)
            {
                this.logger.TryGet()?.Log(Hashed.Error.Connect, this.nestedcommand.Node.ToString());
                return;
            }

            var service = terminal.GetService<IMergerClient>();

            var response = await service.GetInformation().ResponseAsync;
            if (response.IsSuccess && response.Value is { } informationResult)
            {
                this.logger.TryGet()?.Log(informationResult.MergerName);
            }

            /*var token = await terminal.CreateToken(Token.Type.RequestAuthorization);
            if (token == null)
            {
                return;
            }

            var service = terminal.GetService<IRemoteControlService>();
            var response = await service.RequestAuthorization(token).ResponseAsync;
            var result = response.Result;
            this.logger.TryGet()?.Log($"RequestAuthorization: {result}");

            if (result == NetResult.Success)
            {
                result = await service.Restart();
                this.logger.TryGet()?.Log($"Restart: {result}");
            }*/
        }
    }

    private ILogger logger;
    private NetTerminal terminal;
    private MergerNestedcommand nestedcommand;
}
