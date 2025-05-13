// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands.Credential;

[SimpleCommand("show-credentials")]
public class ShowCredentialsCommand : ISimpleCommand
{
    public ShowCredentialsCommand(IUserInterfaceService userInterfaceService, Credentials credentials)
    {
        this.userInterfaceService = userInterfaceService;
        this.credentials = credentials;
    }

    public void Run(string[] args)
    {
        this.userInterfaceService.WriteLine($"Evidences");
        foreach (var evidence in this.credentials.Nodes.ToArray())
        {
            this.userInterfaceService.WriteLine($"{evidence.ToString(Alias.Instance)}");
        }
    }

    private readonly IUserInterfaceService userInterfaceService;
    private readonly Credentials credentials;
}
