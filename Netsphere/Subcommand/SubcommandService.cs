// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere;

namespace LP.Subcommands;

public static partial class SubcommandService
{
    public static bool TryParseNodeAddress(string node, [MaybeNullWhen(false)] out NodeAddress nodeAddress)
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
                Logger.Priority.Error($"Could not parse: {node.ToString()}");
                return false;
            }

            if (!address.IsValid())
            {
                Logger.Priority.Error($"Invalid node address: {node.ToString()}");
                return false;
            }

            nodeAddress = address;
            return true;
        }
    }
}
