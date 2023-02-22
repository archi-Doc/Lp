// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using LP.NetServices.T3CS;
using LP.T3CS;
using Netsphere;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("createcredit")]
public class MergerNestedcommandCreateCredit : ISimpleCommandAsync<CreateCreditOptions>
{
    public MergerNestedcommandCreateCredit(ILogger<MergerNestedcommandCreateCredit> logger, Terminal terminal, MergerNestedcommand nestedcommand, Authority authority)
    {
        this.logger = logger;
        this.terminal = terminal;
        this.nestedcommand = nestedcommand;
        this.authority = authority;
    }

    public async Task RunAsync(CreateCreditOptions options, string[] args)
    {
        var authorityKey = await this.authority.GetKeyAsync(options.Authority);
        if (authorityKey == null)
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, options.Authority);
            return;
        }

        using (var terminal = await this.terminal.CreateAndEncrypt(this.nestedcommand.Node))
        {
            if (terminal == null)
            {
                this.logger.TryGet()?.Log(Hashed.Error.Connect, this.nestedcommand.Node.ToString());
                return;
            }

            var service = terminal.GetService<MergerService>();

            var token = await terminal.CreateToken(Token.Type.CreateCredit);
            if (token == null)
            {
                return;
            }

            authorityKey.SignToken(token);
            var param = new Merger.CreateCreditParams(
                token);
            var response = await service.CreateCredit(param).ResponseAsync;
            if (response.IsSuccess && response.Value is { } result)
            {
                this.logger.TryGet()?.Log(result.ToString());
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
    private Terminal terminal;
    private MergerNestedcommand nestedcommand;
    private Authority authority;
}

public record CreateCreditOptions
{
    [SimpleOption("authority", Description = "Authority name", Required = true)]
    public string Authority { get; init; } = string.Empty;
}
