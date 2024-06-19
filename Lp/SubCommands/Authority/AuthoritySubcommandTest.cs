// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("test")]
public class AuthoritySubcommandTest : ISimpleCommandAsync<AuthoritySubcommandTestOptions>
{
    public AuthoritySubcommandTest(ILogger<AuthoritySubcommandTest> logger, Control control, IUserInterfaceService userInterfaceService)
    {
        this.Control = control;
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
    }

    public async Task RunAsync(AuthoritySubcommandTestOptions option, string[] args)
    {
        if (await this.Control.AuthorityVault.GetAuthority(option.Name) is { } authority)
        {
            var signature = authority.SignData(new Credit(), Array.Empty<byte>());
            if (signature != null)
            {
                this.userInterfaceService.WriteLine(signature.ToString());
                this.userInterfaceService.WriteLine(authority.VerifyData(new Credit(), Array.Empty<byte>(), signature).ToString());
            }
        }
    }

    public Control Control { get; set; }

    private ILogger<AuthoritySubcommandTest> logger;
    private IUserInterfaceService userInterfaceService;
}

public record AuthoritySubcommandTestOptions
{
    [SimpleOption("Name", Description = "Key name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("Pass", Description = "Passphrase")]
    public string? Passphrase { get; init; }

    public override string ToString() => $"{this.Name}";
}
