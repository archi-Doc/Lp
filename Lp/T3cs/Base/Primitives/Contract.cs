// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

#pragma warning disable SA1202 // Elements should be ordered by access

/// <summary>
/// This class stores contracts between Mergers.<br/>
/// One purpose is to retain information associated with a Proof,<br/>
/// and the other is to remove the Proof itself while retaining only the identifier of the authentication information.
/// </summary>
[TinyhandObject]
public readonly partial struct Contract : IEquatable<Contract>, ITinyhandSerializable<Contract>
{
    #region FieldAndProperty

    [Key(0)]
    private readonly object? proofOrIdentifier; // null or ContractableProof or byte[32]

    [Key(1)]
    public readonly Point Partial;

    [Key(2)]
    public readonly Point Total;

    public ContractableProof Proof => this.proofOrIdentifier is ContractableProof proof ? proof : EmptyProof.Instance;

    public bool IsEmpty => this.proofOrIdentifier is null;

    public bool IsProof => this.proofOrIdentifier is ContractableProof;

    public bool IsIdentifier => this.proofOrIdentifier is byte[];

    #endregion

    public Contract(ContractableProof proof, Point partial, Point total)
    {
        this.proofOrIdentifier = proof;
        this.Partial = partial;
        this.Total = total;
    }

    public Contract(ContractableProof proof)
    {
        this.proofOrIdentifier = proof;
    }

    private Contract(byte[] identifier, Point partial, Point total)
    {
        this.proofOrIdentifier = identifier;
        this.Partial = partial;
        this.Total = total;
    }

    static void ITinyhandSerializable<Contract>.Serialize(ref TinyhandWriter writer, scoped ref Contract v, TinyhandSerializerOptions options)
    {
        if (options.IsSignatureMode)
        {
            if (v.proofOrIdentifier is ContractableProof)
            {
                // v.GetIdentifier(writer.Level); // Cannot use a thread static buffer.
                Span<byte> span = stackalloc byte[Identifier.Length];
                v.GetHash(span);
                writer.WriteRaw(span);
            }
            else if (v.proofOrIdentifier is byte[] identifier)
            {
                writer.WriteRaw(identifier);
            }
        }
        else
        {
            writer.WriteArrayHeader(3);

            if (v.proofOrIdentifier is byte[] identifier)
            {
                writer.Write(identifier);
            }
            else if (v.proofOrIdentifier is Proof proof)
            {
                TinyhandSerializer.SerializeObject(ref writer, proof, options);
            }
            else
            {
                writer.WriteNil();
            }

            writer.Write(v.Partial);
            writer.Write(v.Total);
        }
    }

    static unsafe void ITinyhandSerializable<Contract>.Deserialize(ref TinyhandReader reader, scoped ref Contract v, TinyhandSerializerOptions options)
    {
        var numberOfData = reader.ReadArrayHeader();
        options.Security.IncrementDepth(ref reader);
        try
        {
            if (numberOfData-- > 0 && !reader.TryReadNil())
            {
                var c = reader.NextCode;
                if (c == (byte)MessagePackCode.Bin8 || c == (byte)MessagePackCode.Bin16 || c == (byte)MessagePackCode.Bin32)
                {// byte[] Identifier
                    var identifier = reader.ReadBytesToArray();
                    Unsafe.AsRef(in v.proofOrIdentifier) = identifier;
                }
                else
                {// Proof
                    var proof = TinyhandSerializer.DeserializeAndReconstructObject<Proof>(ref reader, options);
                    Unsafe.AsRef(in v.proofOrIdentifier) = proof;
                }
            }

            if (numberOfData-- > 0 && !reader.TryReadNil())
            {
                long vd;
                vd = reader.ReadInt64();
                fixed (long* ptr = &v.Partial)
                {
                    *ptr = vd;
                }
            }

            if (numberOfData-- > 0 && !reader.TryReadNil())
            {
                long vd;
                vd = reader.ReadInt64();
                fixed (long* ptr = &v.Total)
                {
                    *ptr = vd;
                }
            }

            while (numberOfData-- > 0)
            {
                reader.Skip();
            }
        }
        finally
        {
            reader.Depth--;
        }
    }

    static Contract ITinyhandCloneable<Contract>.Clone(scoped ref Contract v, TinyhandSerializerOptions options)
    {// The generated method ignores the members of a type with custom serialization, so a deep copy is implemented here.
        var value = v;
        if (v.proofOrIdentifier is Proof proof)
        {// Proof is a union (abstract) type that does not support Clone(), so it is copied through serialization.
            var writer = TinyhandWriter.CreateFromBytePool();
            try
            {
                TinyhandSerializer.SerializeObject(ref writer, proof, options);
                writer.FlushAndGetReadOnlySpan(out var span, out _);
                var reader = new TinyhandReader(span);
                Unsafe.AsRef(in value.proofOrIdentifier) = TinyhandSerializer.DeserializeObject<Proof>(ref reader, options);
            }
            finally
            {
                writer.Dispose();
            }
        }
        else if (v.proofOrIdentifier is byte[] identifier)
        {
            Unsafe.AsRef(in value.proofOrIdentifier) = identifier.AsSpan().ToArray();
        }

        return value;
    }

    public bool TryGetProof([MaybeNullWhen(false)] out ContractableProof proof)
    {
        if (this.proofOrIdentifier is ContractableProof p)
        {
            proof = p;
            return true;
        }

        proof = default;
        return false;
    }

    public Contract StripProof()
    {
        if (!this.IsProof)
        {// Identifier or null
            return this;
        }

        var identifier = new byte[Identifier.Length];
        this.GetHash(identifier);
        return new(identifier, this.Partial, this.Total);
    }

    public void GetHash(Span<byte> span32)
    {
        if (this.proofOrIdentifier is byte[] identifier)
        {
            identifier.AsSpan().CopyTo(span32);
        }
        else if (this.proofOrIdentifier is Proof proof)
        {
            var writer = TinyhandWriter.CreateFromBytePool();
            try
            {
                TinyhandSerializer.SerializeObject(ref writer, proof, TinyhandSerializerOptions.Signature);
                writer.Write(this.Partial);
                writer.Write(this.Total);

                writer.FlushAndGetReadOnlySpan(out var span, out _);
                Blake3.Get256Span(span, span32);
            }
            finally
            {
                writer.Dispose();
            }
        }
        else
        {
            span32.Clear();
        }
    }

    public bool Equals(Contract other)
    {
        if (this.Partial != other.Partial ||
            this.Total != other.Total)
        {
            return false;
        }

        if (this.proofOrIdentifier is ContractableProof proof1 && other.proofOrIdentifier is ContractableProof proof2)
        {
            return proof1.Equals(proof2);
        }
        else if (this.proofOrIdentifier is byte[] identifier1 && other.proofOrIdentifier is byte[] identifier2)
        {
            return identifier1.AsSpan().SequenceEqual(identifier2.AsSpan());
        }
        else if (this.proofOrIdentifier is null && other.proofOrIdentifier is null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
