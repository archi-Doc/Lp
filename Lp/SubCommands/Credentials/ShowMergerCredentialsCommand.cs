// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Netsphere.Stats;
using SimpleCommandLine;

namespace Lp.Subcommands.Credential;

[SimpleCommand("show-merger-credentials")]
public class ShowMergerCredentialsCommand : ISimpleCommand
{
    public ShowMergerCredentialsCommand(IUserInterfaceService userInterfaceService, Credentials credentials)
    {
        this.userInterfaceService = userInterfaceService;
        this.credentials = credentials;
    }

    public void Run(string[] args)
    {
        foreach (var evidence in this.credentials.MergerCredentials.ToArray())
        {
            this.userInterfaceService.WriteLine($"{evidence.ToString(Alias.Instance)}");
        }
    }

    private readonly IUserInterfaceService userInterfaceService;
    private readonly Credentials credentials;
}
