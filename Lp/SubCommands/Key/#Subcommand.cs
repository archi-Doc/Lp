// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.KeyCommand;

public static class Subcommand
{
    public static void Configure(IUnitConfigurationContext context)
    {
        context.AddSubcommand(typeof(NewSignatureKeySubcommand));
        context.AddSubcommand(typeof(NewEncryptionKeySubcommand));
        context.AddSubcommand(typeof(NewChainKeySubcommand));
    }

    public record NewKeyOptions
    {
        [SimpleOption("Seed", Description = "Seedphrase")]
        public string? Seedphrase { get; init; }
    }
}
