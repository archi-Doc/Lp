// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using LP;
using LP.Services;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("ls")]
public class AuthoritySubcommandLs : ISimpleCommandAsync
{
    public AuthoritySubcommandLs(IConsoleService consoleService, Authority authority)
    {
        this.consoleService = consoleService;
        this.authority = authority;
    }

    public async Task RunAsync(string[] args)
    {
        var names = this.authority.GetNames();
        this.consoleService.WriteLine(string.Join(' ', names));
    }

    private Authority authority;
    private IConsoleService consoleService;
}
