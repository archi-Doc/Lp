﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands.AuthorityCommand;

[SimpleCommand("new-authority")]
public class NewAuthoritySubcommand : ISimpleCommandAsync<AuthoritySubcommandNewOptions>
{
    public NewAuthoritySubcommand(ILogger<NewAuthoritySubcommand> logger, AuthorityControl authorityControl)
    {
        this.logger = logger;
        this.authorityControl = authorityControl;
    }

    public async Task RunAsync(AuthoritySubcommandNewOptions option, string[] args)
    {
        byte[]? seed = default;
        if (option.Seedphrase is not null)
        {
            seed = Seedphrase.TryGetSeed(option.Seedphrase);
            if (seed is null)
            {
                this.logger.TryGet()?.Log(Hashed.Seedphrase.Invalid, option.Seedphrase);
                return;
            }
        }

        var seconds = option.LifetimeInSeconds < 0 ? 0 : option.LifetimeInSeconds;
        var authority = new Authority(seed, option.Lifetime, Mics.FromSeconds(seconds));
        var result = this.authorityControl.NewAuthority(option.Name, option.Passphrase ?? string.Empty, authority);

        if (result)
        {// Success
            this.logger.TryGet()?.Log(Hashed.Authority.Created, option.Name);
        }
        else
        {// Failed
            this.logger.TryGet()?.Log(Hashed.Authority.AlreadyExists, option.Name);
        }
    }

    private readonly ILogger logger;
    private readonly AuthorityControl authorityControl;
}

public record AuthoritySubcommandNewOptions
{
    [SimpleOption("Name", Description = "Key name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("Passphrase", Description = "Passphrase")]
    public string? Passphrase { get; init; }

    [SimpleOption("Seedphrase", Description = "Seedphrase")]
    public string? Seedphrase { get; init; }

    [SimpleOption("Lifetime", Description = "Lifetime")]
    public AuthorityLifecycle Lifetime { get; init; }

    [SimpleOption("Seconds", Description = "Lifetime in seconds")]
    public int LifetimeInSeconds { get; init; }

    public override string ToString() => $"{this.Name}";
}
