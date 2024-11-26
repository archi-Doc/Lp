﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

/// <summary>
/// Order.
/// </summary>
[TinyhandObject]
public sealed partial class Order : IValidatable, IEquatable<Order>
{
    public enum Type
    {
        Evol,
        Ask,
        Bid,
    }

    [TinyhandObject]
    public readonly partial record struct Condition
    {
        public enum Type
        {
            Market,
            Limit,
        }

        public Type ConditionType => this.conditionType;

        public double Ratio => this.ratio;

        [Key(0)]
        private readonly Type conditionType;

        [Key(1)]
        private readonly double ratio;
    }

    public Order()
    {
    }

    [Key(0)]
    public Type OrderType { get; private set; }

    [Key(1)]
    public long Point { get; private set; }

    [Key(2)]
    public Credit Credit { get; private set; } = default!;

    [Key(3)]
    public SignaturePublicKey2 Authority { get; private set; }

    [Key(4)]
    public Condition OrderCondition { get; private set; }

    [Key(5)]
    public long ExpirationMics { get; private set; }

    [Key(6, AddProperty = "Signature", Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    [MaxLength(KeyHelper.SignatureLength)]
    private byte[] signature = Array.Empty<byte>();

    public bool Validate()
    {
        if (!this.Credit.Validate())
        {
            return false;
        }
        else if (!this.Authority.Validate())
        {
            return false;
        }

        return true;
    }

    public bool ValidateAndVerify()
    {
        if (!this.Validate())
        {
            return false;
        }

        try
        {
            var bytes = TinyhandSerializer.Serialize(this, TinyhandSerializerOptions.Signature);
            return this.Authority.Verify(bytes, this.signature);
        }
        catch
        {
            return false;
        }
    }

    public bool Equals(Order? other)
    {
        if (other == null)
        {
            return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = default(HashCode);

        return hash.ToHashCode();
    }
}
