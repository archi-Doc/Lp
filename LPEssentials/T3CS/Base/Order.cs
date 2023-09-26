// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

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
    public readonly partial struct Condition
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

    [Key(6, AddProperty = "Signature", Level = 0)]
    [MaxLength(KeyHelper.SignatureLength)]
    private byte[] signature = [];

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
            return this.Authority.VerifyData(bytes, this.signature);
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
