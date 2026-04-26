// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("new-seedphrase")]
public class NewSeedphraseSubcommand : ISimpleCommand
{
    public NewSeedphraseSubcommand(ILogger<NewSeedphraseSubcommand> logger, IUserInterfaceService userInterfaceService, LpUnit lpUnit)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpUnit = lpUnit;
    }

    public async Task Execute(string[] args, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write($"Create seedphrase");

        var phrase = Seedphrase.Create();
        // this.logger.GetWriter()?.Write($"{phrase}");
        this.userInterfaceService.WriteLine(phrase);

        // var seed = this.seedPhrase.TryGetSeed(phrase);
    }

    private readonly ILogger logger;
    private IUserInterfaceService userInterfaceService;
    private LpUnit lpUnit;
}
