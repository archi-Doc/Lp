// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

#pragma warning disable SA1202 // Elements should be ordered by access

[TinyhandObject]
public readonly partial struct Contract : IEquatable<Contract>
{
    [Key(0)]
    private readonly Proof proof;

    [Key(1)]
    public readonly Point Partial;

    [Key(2)]
    public readonly Point Total;

    public LinkableProof Proof => (LinkableProof)this.proof;

    public Contract(LinkableProof proof, Point partial, Point total)
    {
        this.proof = proof;
        this.Partial = partial;
        this.Total = total;
    }

    public Contract(LinkableProof proof)
    {
        this.proof = proof;
    }

    static void ITinyhandSerializable<Contract>.Serialize(ref TinyhandWriter writer, scoped ref Contract v, TinyhandSerializerOptions options)
    {
        if (options.IsSignatureMode)
        {
            // v.GetIdentifier(writer.Level); // Cannot use a thread static buffer.
            Span<byte> span = stackalloc byte[Identifier.Length];
            v.GetHash(span);
            writer.WriteSpan(span);
        }
        else
        {
            writer.WriteArrayHeader(3);

            TinyhandSerializer.SerializeObject(ref writer, v.proof, options);
            writer.Write(v.Partial);
            writer.Write(v.Total);
        }
    }

    public void GetHash(Span<byte> span32)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, this.proof, TinyhandSerializerOptions.Signature);
            writer.Write(this.Partial);
            writer.Write(this.Total);

            var rentMemory = writer.FlushAndGetRentMemory();
            Blake3.Get256_Span(rentMemory.Span, span32);
            rentMemory.Return();
            writer.WriteSpan(span32);
        }
        finally
        {
            writer.Dispose();
        }
    }

    public bool Equals(Contract other)
        => this.Proof.Equals(other.Proof) && this.Partial == other.Partial && this.Total == other.Total;
}

/*[ValueLinkObject]
public partial class ContractedLinkage : Linkage
{
    public static bool TryCreate(LinkableEvidence evidence1, LinkableEvidence evidence2, [MaybeNullWhen(false)] out MarketableLinkage linkage)
        => TryCreate(() => new(), evidence1, evidence2, out linkage);

    [Link(Primary = true, Unique = true, Type = ChainType.Ordered, TargetMember = nameof(LinkedMics))]
    protected ContractedLinkage()
    {
    }

    public MarketableProof LinkableProof1 => (MarketableProof)this.BaseProof1;

    public MarketableProof LinkableProof2 => (MarketableProof)this.BaseProof2;
}*/
