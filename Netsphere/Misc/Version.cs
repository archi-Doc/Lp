// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Crypto;

[TinyhandObject]
public sealed partial record class Version
{
    public Version()
    {
    }

    [Key(0)]
    public int Identifier { get; set; }

    [Key(1)]
    public long DevelopmentMics { get; set; }

    [Key(2)]
    public int DevelopmentVersionInt { get; set; }

    [Key(3)]
    public long ReleaseMics { get; set; }

    [Key(4)]
    public int ReleaseVersionInt { get; set; }
}
