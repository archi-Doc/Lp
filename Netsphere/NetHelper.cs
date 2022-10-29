// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Netsphere;

public static partial class NetHelper
{
    public static bool TryParseNodeAddress(ILogger? logger, string node, [MaybeNullWhen(false)] out NodeAddress nodeAddress)
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
                logger?.TryGet(LogLevel.Error)?.Log($"Could not parse: {node.ToString()}");
                return false;
            }

            if (!address.IsValid())
            {
                logger?.TryGet(LogLevel.Error)?.Log($"Invalid node address: {node.ToString()}");
                return false;
            }

            nodeAddress = address;
            return true;
        }
    }

    public static bool TryParseNodeInformation(ILogger? logger, string node, [MaybeNullWhen(false)] out NodeInformation nodeInformation)
    {
        nodeInformation = null;
        if (string.Compare(node, "alternative", true) == 0)
        {
            nodeInformation = NodeInformation.Alternative;
            return true;
        }
        else
        {
            if (!NodeInformation.TryParse(node, out var address))
            {
                logger?.TryGet(LogLevel.Error)?.Log($"Could not parse: {node.ToString()}");
                return false;
            }

            if (!address.IsValidPort())
            {
                logger?.TryGet(LogLevel.Error)?.Log($"Invalid port: {node.ToString()}");
                return false;
            }

            nodeInformation = address;
            return true;
        }
    }
}
