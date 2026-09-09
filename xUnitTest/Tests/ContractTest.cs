// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Tinyhand;
using Xunit;

namespace xUnitTest;

public class ContractTest
{
    [Fact]
    public void AnEmptyContractExposesTheEmptyProofAndAZeroHash()
    {
        var contract = default(Contract);
        Assert.True(contract.IsEmpty);
        Assert.False(contract.IsProof);
        Assert.False(contract.IsIdentifier);
        Assert.Same(EmptyProof.Instance, contract.Proof);
        Assert.False(contract.TryGetProof(out _));

        Span<byte> hash = stackalloc byte[32];
        hash.Fill(0xff);
        contract.GetHash(hash);
        Assert.True(hash.TrimStart((byte)0).IsEmpty);
    }

    [Fact]
    public void StrippingAProofKeepsTheHashAndTheAmounts()
    {
        var contract = new Contract(CreateProof(), 12, 34);
        Assert.True(contract.IsProof);
        Assert.True(contract.TryGetProof(out var proof));
        Assert.NotNull(proof);

        Span<byte> expected = stackalloc byte[32];
        contract.GetHash(expected);

        var stripped = contract.StripProof();
        Assert.True(stripped.IsIdentifier);
        Assert.False(stripped.IsProof);
        Assert.Equal(12, stripped.Partial);
        Assert.Equal(34, stripped.Total);

        Span<byte> actual = stackalloc byte[32];
        stripped.GetHash(actual);
        Assert.True(expected.SequenceEqual(actual));

        // Stripping again is a no-op.
        Assert.True(stripped.Equals(stripped.StripProof()));
        Assert.True(default(Contract).Equals(default(Contract).StripProof()));
    }

    [Fact]
    public void TheHashCoversThePartialAndTotalAmounts()
    {
        var proof = CreateProof();
        Span<byte> first = stackalloc byte[32];
        Span<byte> second = stackalloc byte[32];
        new Contract(proof, 1, 2).GetHash(first);
        new Contract(proof, 1, 3).GetHash(second);
        Assert.False(first.SequenceEqual(second));
    }

    [Fact]
    public void EqualityComparesAmountsAndKind()
    {
        var proof = CreateProof();
        Assert.True(new Contract(proof, 1, 2).Equals(new Contract(proof, 1, 2)));
        Assert.False(new Contract(proof, 1, 2).Equals(new Contract(proof, 9, 2)));
        Assert.False(new Contract(proof, 1, 2).Equals(new Contract(proof, 1, 9)));
        Assert.False(new Contract(proof, 1, 2).Equals(new Contract(proof, 1, 2).StripProof()));
        Assert.False(new Contract(proof, 1, 2).Equals(default(Contract)));
        Assert.True(new Contract(proof, 1, 2).StripProof().Equals(new Contract(proof, 1, 2).StripProof()));
    }

    [Fact]
    public void SerializationRoundTripsBothProofAndIdentifierContracts()
    {
        var contract = new Contract(CreateProof(), 5, 6);
        var restored = TinyhandSerializer.Deserialize<Contract>(TinyhandSerializer.Serialize(contract));
        Assert.True(restored.IsProof);
        Assert.Equal(5, restored.Partial);
        Assert.Equal(6, restored.Total);

        var stripped = contract.StripProof();
        var restoredStripped = TinyhandSerializer.Deserialize<Contract>(TinyhandSerializer.Serialize(stripped));
        Assert.True(restoredStripped.IsIdentifier);
        Assert.True(stripped.Equals(restoredStripped));

        var restoredEmpty = TinyhandSerializer.Deserialize<Contract>(TinyhandSerializer.Serialize(default(Contract)));
        Assert.True(restoredEmpty.IsEmpty);
    }

    private static TestLinkageProof CreateProof()
    {
        var key = SeedKey.NewSignature();
        var publicKey = key.GetSignaturePublicKey();
        var proof = new TestLinkageProof(publicKey, new Value(publicKey, 1, new Credit(default, [publicKey])));
        Assert.True(key.TrySign(proof, 60));
        return proof;
    }
}
