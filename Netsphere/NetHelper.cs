// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Netsphere;

public static partial class NetHelper
{
    public static bool TryParseDualAddress(ILogger? logger, string source, [MaybeNullWhen(false)] out DualAddress address)
    {
        address = default;
        if (string.Compare(source, "alternative", true) == 0)
        {
            address = DualAddress.Alternative;
            return true;
        }
        else
        {
            if (!DualAddress.TryParse(source, out address))
            {
                logger?.TryGet(LogLevel.Error)?.Log($"Could not parse: {source.ToString()}");
                return false;
            }

            if (!address.Validate())
            {
                logger?.TryGet(LogLevel.Error)?.Log($"Invalid node address: {source.ToString()}");
                return false;
            }

            return true;
        }
    }

    public static bool TryParseNodeInformation(ILogger? logger, string source, [MaybeNullWhen(false)] out DualNode node)
    {
        node = default;
        if (string.Compare(source, "alternative", true) == 0)
        {
            node = DualNode.Alternative;
            return true;
        }
        else
        {
            if (!DualNode.TryParse(source, out var address))
            {
                logger?.TryGet(LogLevel.Error)?.Log($"Could not parse: {source.ToString()}");
                return false;
            }

            if (!address.Address.Validate())
            {
                logger?.TryGet(LogLevel.Error)?.Log($"Invalid port: {source.ToString()}");
                return false;
            }

            node = address;
            return true;
        }
    }
}
