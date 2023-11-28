// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Transmission;

public abstract class Transmission
{
    public Transmission(uint transmissionId)
    {
        this.TransmissionId = transmissionId;
    }

    public uint TransmissionId { get; }
}
