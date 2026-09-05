// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
    public SignaturePublicKey Authority { get; private set; }

    [Key(4)]
    public Condition OrderCondition { get; private set; }

    [Key(5)]
    public long ExpirationMics { get; private set; }

    [Key(6, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    [MaxLength(CryptoSign.SignatureSize)]
    public partial byte[] Signature { get; private set; } = [];

    public bool Validate()
    {
        if (this.Credit is null || !this.Credit.Validate())
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

        var writer = TinyhandWriter.CreateFromBytePool();
        writer.Level = TinyhandWriter.DefaultSignatureLevel;
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, this, TinyhandSerializerOptions.Signature);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            return this.Authority.Verify(span, this.Signature);
        }
        catch
        {
            return false;
        }
        finally
        {
            writer.Dispose();
        }
    }

    public bool Equals(Order? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.OrderType == other.OrderType &&
            this.Point == other.Point &&
            EqualityComparer<Credit>.Default.Equals(this.Credit, other.Credit) &&
            this.Authority.Equals(other.Authority) &&
            this.OrderCondition.Equals(other.OrderCondition) &&
            this.ExpirationMics == other.ExpirationMics &&
            this.Signature.AsSpan().SequenceEqual(other.Signature);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.OrderType, this.Point, this.Credit, this.Authority, this.OrderCondition, this.ExpirationMics);
    }
}
