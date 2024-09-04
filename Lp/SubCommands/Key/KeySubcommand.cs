// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

public static class KeySubcommand
{
    public static void Configure(IUnitConfigurationContext context)
    {
        context.AddCommand(typeof(NewSignatureKeySubcommand));
        context.AddCommand(typeof(NewNodeKeySubcommand));
    }

    public record NewKeyOptions
    {
        [SimpleOption("Seed", Description = "Seedphrase")]
        public string? Seedphrase { get; init; }
    }
}
