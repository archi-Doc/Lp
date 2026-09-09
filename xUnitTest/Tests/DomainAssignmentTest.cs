// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp;
using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using Tinyhand;
using Xunit;

namespace xUnitTest;

public class DomainAssignmentTest
{
    [Fact]
    public void TheDomainHashFollowsTheCertificateProof()
    {
        var first = CreateAssignment();
        var second = new DomainAssignment("other name", "other code", first.CertificateProof);

        // Name and code are not part of the identity of a domain.
        Assert.Equal(first.GetDomainHash(), second.GetDomainHash());
        Assert.NotEqual(first.GetDomainHash(), CreateAssignment().GetDomainHash());
    }

    [Fact]
    public void ValidationFollowsTheCertificateProof()
    {
        var assignment = CreateAssignment();
        Assert.False(assignment.Validate(ValidationOption.IgnoreExpiration));

        var key = SeedKey.NewSignature();
        var publicKey = key.GetSignaturePublicKey();
        var value = new Value(publicKey, 1, new Credit(default, [publicKey]));
        var proof = new CertificateProof(new MergedProof(value), new NetNode());
        Assert.True(key.TrySign(proof, 60));
        Assert.Equal(proof.Validate(ValidationOption.IgnoreExpiration), new DomainAssignment("n", string.Empty, proof).Validate(ValidationOption.IgnoreExpiration));
    }

    [Fact]
    public void SerializationRoundTripsThroughTheTextFormat()
    {
        var assignment = CreateAssignment();
        var restored = StringHelper.DeserializeFromString<DomainAssignment>(assignment.ToString());
        Assert.NotNull(restored);
        Assert.Equal(assignment.Name, restored.Name);
        Assert.Equal(assignment.Code, restored.Code);
        Assert.Equal(assignment.GetDomainHash(), restored.GetDomainHash());
    }

    [Fact]
    public void BinarySerializationRoundTrips()
    {
        var assignment = CreateAssignment();
        var restored = TinyhandSerializer.Deserialize<DomainAssignment>(TinyhandSerializer.Serialize(assignment));
        Assert.NotNull(restored);
        Assert.Equal(assignment.GetDomainHash(), restored.GetDomainHash());
    }

    private static DomainAssignment CreateAssignment()
    {
        var key = SeedKey.NewSignature().GetSignaturePublicKey();
        var value = new Value(key, 1, new Credit(default, [key]));
        var proof = new CertificateProof(new MergedProof(value), new NetNode());
        return new DomainAssignment(Guid.NewGuid().ToString("N"), string.Empty, proof);
    }
}
