﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

/// <summary>
/// Immutable linkage object.
/// </summary>
[TinyhandObject]
public sealed partial class Linkage : IValidatable // , IEquatable<Linkage>
{
    public const long MaxPoint = 1_000_000_000_000_000_000; // k, m, g, t, p, e, 1z
    public const long MinPoint = -MaxPoint;
    public const int MaxMergers = 4;

    public Linkage()
    {
    }

    [Key(0)]
    public long Point { get; private set; }

    [Key(1)]
    public AuthorityPublicKey Owner { get; private set; } = default!;

    [Key(2)]
    public AuthorityPublicKey Originator { get; private set; } = default!;

    [Key(3, PropertyName = "Mergers")]
    [MaxLength(MaxMergers)]
    private AuthorityPublicKey[] mergers = default!;

    /*[Key(4, Marker = true, PropertyName = "Signs")]
    [MaxLength(MaxMergers + 1, Authority.PublicKeyLength)]
    private byte[][] signs = default!;*/

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
        else if (this.mergers == null || this.mergers.Length > MaxMergers)
        {
            return false;
        }

        /*else if (this.signs == null || this.signs.Length != (1 + this.mergers.Length))
        {
            return false;
        }*/

        for (var i = 0; i < this.mergers.Length; i++)
        {
            if (this.mergers[i] == null || !this.mergers[i].Validate())
            {
                return false;
            }
        }

        /*for (var i = 0; i < this.signs.Length; i++)
        {
            if (this.signs[i] == null || this.signs[i].Length != Authority.PublicKeyLength)
            {
                return false;
            }
        }*/

        return true;
    }
}
