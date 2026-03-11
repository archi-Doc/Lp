// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerClient;

[SimpleCommand("info")]
public class InfoCommand : ISimpleCommandAsync
{
    public InfoCommand(ILogger<InfoCommand> logger, NetTerminal terminal, NestedCommand nestedcommand)
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
            this.logger.GetWriter(LogLevel.Error)?.Write(Hashed.Authority.NotFound, options.Authority);
            return;
        }*/

        if (this.nestedcommand.RobustConnection is not { } robustConnection)
        {
            return;
        }

        this.logger.GetWriter()?.Write(string.Empty);
        if (await robustConnection.Get(this.logger) is not { } connection)
        {
            return;
        }

        var service = connection.GetService<IMergerService>();

        var response = await service.GetInformation();
        if (response is { } informationResult)
        {
            this.logger.GetWriter()?.Write(informationResult.MergerName);
        }

        /*var token = await terminal.CreateToken(Token.Type.RequestAuthorization);
        if (token == null)
        {
            return;
        }

        var service = terminal.GetService<IRemoteControlService>();
        var response = await service.RequestAuthorization(token).ResponseAsync;
        var result = response.Result;
        this.logger.GetWriter()?.Write($"RequestAuthorization: {result}");

        if (result == NetResult.Success)
        {
            result = await service.Restart();
            this.logger.GetWriter()?.Write($"Restart: {result}");
        }*/
    }

    private ILogger logger;
    private NetTerminal terminal;
    private NestedCommand nestedcommand;
}
