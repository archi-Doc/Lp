// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Version;

[TinyhandObject]
public sealed partial record class VersionInfo
{
    public VersionInfo()
    {
    }

    public enum Kind : byte
    {
        Development,
        Release,
    }

    [Key(0)]
    public int VersionIdentifier { get; private set; }

    [Key(1)]
    public Kind VersionKind { get; private set; }

    [Key(2)]
    public long VersionMics { get; private set; }

    [Key(3)]
    public int VersionInt { get; private set; }
}
