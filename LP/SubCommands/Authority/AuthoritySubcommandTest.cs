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
        if (await this.Control.Authority.GetKeyAsync(option.Name) is { } authorityKey)
        {
            var signature = authorityKey.SignData(new Credit(), Array.Empty<byte>());
            Console.WriteLine(signature.ToString());
            Console.WriteLine(authorityKey.VerifyData(new Credit(), Array.Empty<byte>(), signature));
        }
    }

    public Control Control { get; set; }

    private ILogger<AuthoritySubcommandTest> logger;
}

public record AuthoritySubcommandTestOptions
{
    [SimpleOption("name", Description = "Key name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("pass", Description = "Passphrase")]
    public string? Passphrase { get; init; }

    public override string ToString() => $"{this.Name}";
}
