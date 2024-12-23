// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Subcommands.Relay;

public static class Subcommand
{
    public static void Configure(IUnitConfigurationContext context)
    {
        context.AddSubcommand(typeof(ShowIncomingRelaySubcommand));
        context.AddSubcommand(typeof(ShowRelayInformationSubcommand));
        context.AddSubcommand(typeof(ShowRelayExchangeSubcommand));
        context.AddSubcommand(typeof(AddCertificateRelaySubcommand));
    }
}
