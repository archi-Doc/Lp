// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Tinyhand;
using Xunit;

namespace xUnitTest;

public class OrderTest
{
    [Fact]
    public void SignatureVerificationPreservesSerializedSigningFormat()
    {
        var key = SeedKey.NewSignature();
        var publicKey = key.GetSignaturePublicKey();
        var order = new Order();
        typeof(Order).GetProperty(nameof(Order.Credit))!.SetValue(order, new Credit(default, [publicKey]));
        typeof(Order).GetProperty(nameof(Order.Authority))!.SetValue(order, publicKey);
        var signature = new byte[64];
        key.Sign(TinyhandSerializer.Serialize(order, TinyhandSerializerOptions.Signature), signature);
        typeof(Order).GetProperty(nameof(Order.Signature))!.SetValue(order, signature);
        Assert.True(order.ValidateAndVerify());
        typeof(Order).GetProperty(nameof(Order.Point))!.SetValue(order, 123L);
        Assert.False(order.ValidateAndVerify());
    }

    [Fact]
    public void UninitializedOrderFailsValidationWithoutThrowing()
    {
        Assert.False(new Order().Validate());
        Assert.False(new Order().ValidateAndVerify());
    }

    [Theory]
    [InlineData(nameof(Order.OrderType), Order.Type.Ask)]
    [InlineData(nameof(Order.Point), 123L)]
    [InlineData(nameof(Order.ExpirationMics), 456L)]
    public void EqualityComparesOrderContents(string property, object value)
    {
        var original = new Order();
        var other = new Order();
        Assert.True(original.Equals(other));
        Assert.Equal(original.GetHashCode(), other.GetHashCode());
        typeof(Order).GetProperty(property)!.SetValue(other, value);
        Assert.False(original.Equals(other));
        Assert.False(original.Equals(null));
        var copy = TinyhandSerializer.Deserialize<Order>(TinyhandSerializer.Serialize(other));
        Assert.NotNull(copy);
        Assert.Equal(other.Point, copy.Point);
        Assert.Equal(other.OrderType, copy.OrderType);
    }

    [Fact]
    public void EqualityComparesSignaturesByContent()
    {
        var left = new Order();
        var right = new Order();
        typeof(Order).GetProperty(nameof(Order.Signature))!.SetValue(left, new byte[] { 1, 2 });
        typeof(Order).GetProperty(nameof(Order.Signature))!.SetValue(right, new byte[] { 1, 2 });
        Assert.True(left.Equals(right));
        right.Signature[0] = 3;
        Assert.False(left.Equals(right));
    }
}
