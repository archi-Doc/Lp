// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("new-seedphrase")]
public class NewSeedphraseSubcommand : ISimpleCommandAsync
{
    public NewSeedphraseSubcommand(ILogger<NewSeedphraseSubcommand> logger, IUserInterfaceService userInterfaceService, Control control)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.control = control;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet()?.Log($"Create seedphrase");

        var phrase = Seedphrase.Create();
        // this.logger.TryGet()?.Log($"{phrase}");
        this.userInterfaceService.WriteLine(phrase);

        // var seed = this.seedPhrase.TryGetSeed(phrase);
    }

    private readonly ILogger logger;
    private IUserInterfaceService userInterfaceService;
    private Control control;
}
