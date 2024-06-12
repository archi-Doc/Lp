// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Version;

[TinyhandObject]
public sealed partial record class VersionInfo
{
    public VersionInfo()
    {
    }

    [Key(0)]
    public int VersionIdentifier { get; private set; }

    [Key(1)]
    public long DevelopmentMics { get; private set; }

    [Key(2)]
    public int DevelopmentVersionInt { get; private set; }

    [Key(3)]
    public long ReleaseMics { get; private set; }

    [Key(4)]
    public int ReleaseVersionInt { get; private set; }
}
