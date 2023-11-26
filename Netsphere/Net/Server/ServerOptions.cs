// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Server;

public record ServerOptions
{
    public int MaxTransmissions { get; init; } = 4;

    public int TransmissionWindow { get; init; } = 4;
}
