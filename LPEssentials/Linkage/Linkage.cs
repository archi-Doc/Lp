// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

/// <summary>
/// Immutable linkage object.
/// </summary>
[TinyhandObject]
public partial class Linkage : IValidatable // , IEquatable<Linkage>, IComparable<Linkage>
{
    public const int MaxMergers = 4;

    public Linkage()
    {
        this.Point = 0;
        this.Owner = default!;
        this.Mergers = default!;
        this.Signs = default!;
    }

    public bool Validate()
    {
        return true;
    }

    [Key(0)]
    public double Point { get; private set; }

    [Key(1)]
    public Authority Owner { get; private set; }

    [Key(2)]
    public Authority[] Mergers { get; private set; }

    [Key(3, Marker = true)]
    public byte[][] Signs { get; private set; }
}
