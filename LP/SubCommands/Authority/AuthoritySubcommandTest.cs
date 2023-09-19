// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using SimpleCommandLine;

namespace LP.Subcommands;

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
        if (await this.Control.Authority.GetAuthority(option.Name) is { } authoritySeed)
        {
            var signature = authoritySeed.SignData(new Credit(), Array.Empty<byte>());
            if (signature != null)
            {
                this.userInterfaceService.WriteLine(signature.ToString());
                this.userInterfaceService.WriteLine(authoritySeed.VerifyData(new Credit(), Array.Empty<byte>(), signature).ToString());
            }
        }
    }

    public Control Control { get; set; }

    private ILogger<AuthoritySubcommandTest> logger;
    private IUserInterfaceService userInterfaceService;
}

public record AuthoritySubcommandTestOptions
{
    [SimpleOption("name", Description = "Key name", Required = true)]
    public string Name { get; init; } = string.Empty;

    [SimpleOption("pass", Description = "Passphrase")]
    public string? Passphrase { get; init; }

    public override string ToString() => $"{this.Name}";
}
