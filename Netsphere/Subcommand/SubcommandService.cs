// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere;

namespace LP.Subcommands;

public static partial class SubcommandService
{
    public static bool TryParseNodeAddress(ILoggerSource logger, string node, [MaybeNullWhen(false)] out NodeAddress nodeAddress)
    {
        nodeAddress = null;
        if (string.Compare(node, "alternative", true) == 0)
        {
            nodeAddress = NodeAddress.Alternative;
            return true;
        }
        else
        {
            if (!NodeAddress.TryParse(node, out var address))
            {
                logger.TryGet(LogLevel.Error)?.Log($"Could not parse: {node.ToString()}");
                return false;
            }

            if (!address.IsValid())
            {
                logger.TryGet(LogLevel.Error)?.Log($"Invalid node address: {node.ToString()}");
                return false;
            }

            nodeAddress = address;
            return true;
        }
    }
}
