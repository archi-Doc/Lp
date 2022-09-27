// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("new")]
public class AuthoritySubcommandNew : ISimpleCommandAsync<AuthoritySubcommandNewOptions>
{
    public AuthoritySubcommandNew(ILogger<AuthoritySubcommandNew> logger, Control control)
    {
        this.Control = control;
        this.logger = logger;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.Control.KeyVault.GetNames();
        Console.WriteLine(string.Join(' ', names));
    }

    public async Task RunAsync(AuthoritySubcommandNewOptions option, string[] args)
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
        this.Control.KeyVault.SerializeAndTryAdd(name, pri);

        this.logger.TryGet()?.Log("Key created:");
        this.logger.TryGet()?.Log(pri.ToString());
    }

    public Control Control { get; set; }

    private ILogger<AuthoritySubcommandNew> logger;
}

public record AuthoritySubcommandNewOptions
{
    [SimpleOption("name", description: "Key name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("pass", description: "Passphrase")]
    public string? Passphrase { get; init; }

    public override string ToString() => $"{this.Name}";
}
