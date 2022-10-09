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

    public async Task RunAsync(AuthoritySubcommandNewOptions option, string[] args)
    {
        var seconds = option.Seconds < 0 ? 0 : option.Seconds;
        var authorityInfo = new AuthorityInfo(option.Seedphrase, option.Lifetime, Mics.FromSeconds(seconds));
        var result = this.Control.Authority.NewAuthority(option.Name, option.Passphrase ?? string.Empty, authorityInfo);

        if (result == AuthorityResult.Success)
        {
            this.logger.TryGet()?.Log(Hashed.Authority.Created, option.Name);
        }
        else if (result == AuthorityResult.AlreadyExists)
        {
            this.logger.TryGet()?.Log(Hashed.Authority.AlreadyExists, option.Name);
        }
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

    [SimpleOption("seed", description: "Seedphrase")]
    public string? Seedphrase { get; init; }

    [SimpleOption("lifetime", description: "Lifetime")]
    public AuthorityLifetime Lifetime { get; init; }

    [SimpleOption("seconds", description: "Lifetime in seconds")]
    public int Seconds { get; init; }

    public override string ToString() => $"{this.Name}";
}
