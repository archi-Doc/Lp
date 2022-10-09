// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("test")]
public class AuthoritySubcommandTest : ISimpleCommandAsync<AuthoritySubcommandTestOptions>
{
    public AuthoritySubcommandTest(ILogger<AuthoritySubcommandTest> logger, Control control)
    {
        this.Control = control;
        this.logger = logger;
    }

    public async Task RunAsync(AuthoritySubcommandTestOptions option, string[] args)
    {
        PrivateKey pri;

        if (option.Passphrase == null)
        {
            pri = PrivateKey.Create(option.Name);
        }
        else
        {
            pri = PrivateKey.Create(option.Name, option.Passphrase);
        }

        var name = "Authority\\" + option.Name;
        this.Control.Vault.SerializeAndTryAdd(name, pri);

        this.logger.TryGet()?.Log("Key created:");
        this.logger.TryGet()?.Log(pri.ToString());

        if (this.Control.Authority.TryGetInterface(option.Name, out var authorityInterface) == AuthorityResult.Success)
        {
            await authorityInterface.TrySignData(new Credit(), Array.Empty<byte>());
        }
    }

    public Control Control { get; set; }

    private ILogger<AuthoritySubcommandTest> logger;
}

public record AuthoritySubcommandTestOptions
{
    [SimpleOption("name", description: "Key name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("pass", description: "Passphrase")]
    public string? Passphrase { get; init; }

    public override string ToString() => $"{this.Name}";
}
