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
        if (this.Control.Authority.TryGetInterface(option.Name, 0, out var authorityInterface) == AuthorityResult.Success)
        {
            var result = await authorityInterface.SignData(new Credit(), Array.Empty<byte>());
            Console.WriteLine(result.ToString());
            Console.WriteLine(await authorityInterface.VerifyData(new Credit(), Array.Empty<byte>(), result.Signature));
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
