// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("new-seedphrase")]
public class NewSeedphraseSubcommand : ISimpleCommandAsync
{
    public NewSeedphraseSubcommand(ILogger<NewSeedphraseSubcommand> logger, IUserInterfaceService userInterfaceService, Control control, Seedphrase seedPhrase)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.control = control;
        this.seedPhrase = seedPhrase;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet()?.Log($"Create seedphrase");

        var phrase = this.seedPhrase.Create();
        // this.logger.TryGet()?.Log($"{phrase}");
        this.userInterfaceService.WriteLine(phrase);

        // var seed = this.seedPhrase.TryGetSeed(phrase);
    }

    private readonly ILogger logger;
    private IUserInterfaceService userInterfaceService;
    private Control control;
    private Seedphrase seedPhrase;
}
