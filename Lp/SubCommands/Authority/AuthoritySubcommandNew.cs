// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("new")]
public class AuthoritySubcommandNew : ISimpleCommandAsync<AuthoritySubcommandNewOptions>
{
    public AuthoritySubcommandNew(ILogger<AuthoritySubcommandNew> logger, AuthorityVault authorityVault, Seedphrase seedphrase)
    {
        this.logger = logger;
        this.authorityVault = authorityVault;
        this.seedphrase = seedphrase;
    }

    public async Task RunAsync(AuthoritySubcommandNewOptions option, string[] args)
    {
        byte[]? seed = default;
        if (option.Seedphrase is not null)
        {
            seed = this.seedphrase.TryGetSeed(option.Seedphrase);
            if (seed is null)
            {
                this.logger.TryGet()?.Log(Hashed.Seedphrase.Invalid, option.Seedphrase);
                return;
            }
        }

        var seconds = option.LifetimeInSeconds < 0 ? 0 : option.LifetimeInSeconds;
        var authorityInfo = new Authority(seed, option.Lifetime, Mics.FromSeconds(seconds));
        var result = this.authorityVault.NewAuthority(option.Name, option.Passphrase ?? string.Empty, authorityInfo);

        if (result == AuthorityResult.Success)
        {
            this.logger.TryGet()?.Log(Hashed.Authority.Created, option.Name);
        }
        else if (result == AuthorityResult.AlreadyExists)
        {
            this.logger.TryGet()?.Log(Hashed.Authority.AlreadyExists, option.Name);
        }
    }

    private readonly ILogger logger;
    private readonly AuthorityVault authorityVault;
    private readonly Seedphrase seedphrase;
}

public record AuthoritySubcommandNewOptions
{
    [SimpleOption("name", Description = "Key name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("pass", Description = "Passphrase")]
    public string? Passphrase { get; init; }

    [SimpleOption("seed", Description = "Seedphrase")]
    public string? Seedphrase { get; init; }

    [SimpleOption("lifetime", Description = "Lifetime")]
    public AuthorityLifetime Lifetime { get; init; }

    [SimpleOption("seconds", Description = "Lifetime in seconds")]
    public int LifetimeInSeconds { get; init; }

    public override string ToString() => $"{this.Name}";
}
