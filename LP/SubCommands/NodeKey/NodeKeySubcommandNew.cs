// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("new")]
public class NodeKeySubcommandNew : ISimpleCommand<NodeKeySubcommandNewOptions>
{
    public NodeKeySubcommandNew(ILogger<NodeKeySubcommandNew> logger, IUserInterfaceService userInterfaceService, Seedphrase seedPhrase)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.seedPhrase = seedPhrase;
    }

    public void Run(NodeKeySubcommandNewOptions options, string[] args)
    {
        this.logger.TryGet()?.Log("New node key");

        NodePrivateKey nodeKey;
        if (string.IsNullOrEmpty(options.Seedphrase))
        {
            nodeKey = NodePrivateKey.Create();
        }
        else
        {
            var seed = this.seedPhrase.TryGetSeed(options.Seedphrase);
            if (seed == null)
            {
                this.logger.TryGet()?.Log(Hashed.Seedphrase.Invalid, options.Seedphrase);
                return;
            }

            nodeKey = NodePrivateKey.Create(seed);
        }

        this.userInterfaceService.WriteLine(nodeKey.UnsafeToString());
        this.logger.TryGet()?.Log(nodeKey.ToPublicKey().ToString());
    }

    private ILogger<NodeKeySubcommandNew> logger;
    private IUserInterfaceService userInterfaceService;
    private Seedphrase seedPhrase;
}

public record NodeKeySubcommandNewOptions
{
    [SimpleOption("seed", Description = "Seedphrase")]
    public string? Seedphrase { get; init; }
}
