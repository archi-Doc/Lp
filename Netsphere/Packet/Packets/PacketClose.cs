// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[TinyhandObject]
internal partial class PacketCloseObsolete : IPacketObsolete
{
    public PacketIdObsolete PacketId => PacketIdObsolete.Close;
}
