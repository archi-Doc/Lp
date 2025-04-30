// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.KeyCommand;

public static class Subcommand
{
    public static void Configure(IUnitConfigurationContext context)
    {
        context.AddSubcommand(typeof(NewEncryptionKeySubcommand));
        context.AddSubcommand(typeof(NewMasterKeySubcommand));
        context.AddSubcommand(typeof(NewSignatureKeySubcommand));
    }

    public record NewKeyOptions
    {
        [SimpleOption("Seedphrase", Description = "Seedphrase")]
        public string? Seedphrase { get; init; }
    }
}
