// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

/// <summary>
/// Immutable linkage object.
/// </summary>
[TinyhandObject]
public partial class Linkage : IValidatable // , IEquatable<Linkage>, IComparable<Linkage>
{
    public const double MaxPoint = 1.0E15;
    public const double MinPoint = -1.0E15;
    public const int MaxMergers = 4;

    public enum Type
    {
        A,
    }

    public Linkage()
    {
        this.Point = 0;
        this.Owner = default!;
        this.Mergers = default!;
        this.Signs = default!;
    }

    public bool Validate()
    {
        if (this.Point < MinPoint || this.Point > MaxPoint)
        {
            return false;
        }
        else if (this.Owner == null || !this.Owner.Validate())
        {
            return false;
        }
        else if (this.Mergers == null || this.Mergers.Length > MaxMergers)
        {
            return false;
        }
        else if (this.Signs == null || this.Signs.Length != (1 + this.Mergers.Length))
        {
            return false;
        }

        for (var i = 0; i < this.Mergers.Length; i++)
        {
            if (this.Mergers[i] == null || !this.Mergers[i].Validate())
            {
                return false;
            }
        }

        for (var i = 0; i < this.Signs.Length; i++)
        {
            if (this.Signs[i] == null || this.Signs[i].Length != Authority.PublicKeyLength)
            {
                return false;
            }
        }

        return true;
    }

    [Key(0)]
    public double Point { get; private set; }

    [Key(1)]
    public Type LinkageType { get; private set; }

    [Key(2)]
    public Authority Owner { get; private set; }

    [Key(3)]
    public Authority[] Mergers { get; private set; }

    [Key(4, Marker = true)]
    public byte[][] Signs { get; private set; }
}
